using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PrenominaApi.Attributes;
using PrenominaApi.Models.Prenomina;
using System.Reflection;
using System.Text;

namespace PrenominaApi.Data
{
    /// <summary>
    /// Extrae información de ChangeTracker y produce entradas de AuditLog
    /// con una descripción en español legible por humanos.
    /// </summary>
    internal static class AuditLogBuilder
    {
        private static readonly HashSet<string> SystemProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedAt", "UpdatedAt", "DeletedAt", "Id"
        };

        /// <summary>
        /// Snapshot previo a SaveChanges. Se toma antes de aplicar conversión de Delete→Modified,
        /// para distinguir borrados lógicos de modificaciones.
        /// </summary>
        public sealed class PendingAudit
        {
            public required object Entity { get; init; }
            public required Type EntityType { get; init; }
            public required AuditableAttribute Attribute { get; init; }
            public required EntityState OriginalState { get; init; }
            public required string? PrimaryKeyPropertyName { get; init; }
            public string? Identifier { get; set; }
            public List<(string Label, string? Old, string? New)> Changes { get; } = new();
        }

        public static List<PendingAudit> Capture(ChangeTracker changeTracker)
        {
            var pending = new List<PendingAudit>();

            foreach (var entry in changeTracker.Entries())
            {
                if (entry.Entity is AuditLog) continue;

                var type = entry.Entity.GetType();
                var attr = type.GetCustomAttribute<AuditableAttribute>();
                if (attr == null) continue;

                if (entry.State != EntityState.Added &&
                    entry.State != EntityState.Modified &&
                    entry.State != EntityState.Deleted)
                {
                    continue;
                }

                // Un Update que setea DeletedAt de null → valor es un soft-delete explícito.
                // Lo tratamos como Deleted para que el audit lo describa correctamente.
                var effectiveState = entry.State;
                if (entry.State == EntityState.Modified)
                {
                    var deletedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "DeletedAt");
                    if (deletedAtProp != null && deletedAtProp.IsModified &&
                        deletedAtProp.OriginalValue == null && deletedAtProp.CurrentValue != null)
                    {
                        effectiveState = EntityState.Deleted;
                    }
                }

                var audit = new PendingAudit
                {
                    Entity = entry.Entity,
                    EntityType = type,
                    Attribute = attr,
                    OriginalState = effectiveState,
                    PrimaryKeyPropertyName = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name,
                    Identifier = ExtractIdentifier(entry.Entity, attr)
                };

                if (effectiveState == EntityState.Modified)
                {
                    foreach (var prop in entry.Properties)
                    {
                        if (!prop.IsModified) continue;

                        var propName = prop.Metadata.Name;
                        if (SystemProperties.Contains(propName)) continue;

                        var propInfo = type.GetProperty(propName);
                        var propAttr = propInfo?.GetCustomAttribute<AuditPropertyAttribute>();
                        if (propAttr?.Ignore == true) continue;

                        var oldValue = FormatValue(prop.OriginalValue, propAttr);
                        var newValue = FormatValue(prop.CurrentValue, propAttr);

                        if (oldValue == newValue) continue;

                        var label = string.IsNullOrEmpty(propAttr?.Label) ? propName : propAttr.Label;
                        audit.Changes.Add((label, oldValue, newValue));
                    }

                    // Si no hay cambios reales (ej: solo UpdatedAt), no auditar.
                    if (audit.Changes.Count == 0) continue;
                }

                pending.Add(audit);
            }

            return pending;
        }

        public static IEnumerable<AuditLog> Build(IEnumerable<PendingAudit> pending, Guid byUserId)
        {
            foreach (var p in pending)
            {
                var action = p.OriginalState switch
                {
                    EntityState.Added => "Creó",
                    EntityState.Modified => "Modificó",
                    EntityState.Deleted => "Eliminó",
                    _ => "Modificó"
                };

                var identifier = string.IsNullOrEmpty(p.Identifier) ? "" : $" \"{p.Identifier}\"";
                var description = $"{action} {p.Attribute.Label}{identifier}";

                string oldValue = "";
                string newValue = "";

                if (p.OriginalState == EntityState.Modified && p.Changes.Count > 0)
                {
                    var diff = string.Join(", ",
                        p.Changes.Select(c => $"{c.Label}: {c.Old ?? "(vacío)"} → {c.New ?? "(vacío)"}"));
                    description = $"{description} — {diff}";

                    oldValue = SerializeChanges(p.Changes, useOld: true);
                    newValue = SerializeChanges(p.Changes, useOld: false);
                }
                else if (p.OriginalState == EntityState.Added)
                {
                    newValue = SerializeSnapshot(p.Entity);
                }
                else if (p.OriginalState == EntityState.Deleted)
                {
                    oldValue = SerializeSnapshot(p.Entity);
                }

                yield return new AuditLog
                {
                    SectionCode = p.Attribute.SectionCode,
                    RecordId = ExtractPrimaryKey(p.Entity, p.EntityType, p.PrimaryKeyPropertyName) ?? "",
                    Description = description,
                    OldValue = Truncate(oldValue, 4000),
                    NewValue = Truncate(newValue, 4000),
                    ByUserId = byUserId,
                };
            }
        }

        private static string? ExtractIdentifier(object entity, AuditableAttribute attr)
        {
            var type = entity.GetType();
            foreach (var propName in attr.IdentifierProperties)
            {
                var prop = type.GetProperty(propName);
                if (prop == null) continue;
                var value = prop.GetValue(entity);
                if (value is string s && !string.IsNullOrWhiteSpace(s)) return s;
                if (value != null && !string.IsNullOrWhiteSpace(value.ToString())) return value.ToString();
            }
            return null;
        }

        private static string? ExtractPrimaryKey(object entity, Type type, string? pkPropertyName)
        {
            var pkName = pkPropertyName ?? "Id";
            var idProp = type.GetProperty(pkName) ?? type.GetProperty("Id");
            return idProp?.GetValue(entity)?.ToString();
        }

        private static string? FormatValue(object? value, AuditPropertyAttribute? attr)
        {
            if (attr?.Sensitive == true) return "***";
            if (value == null) return null;

            return value switch
            {
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                DateOnly d => d.ToString("yyyy-MM-dd"),
                TimeOnly t => t.ToString("HH:mm:ss"),
                bool b => b ? "sí" : "no",
                _ => value.ToString()
            };
        }

        private static string SerializeChanges(
            IEnumerable<(string Label, string? Old, string? New)> changes,
            bool useOld)
        {
            var sb = new StringBuilder();
            foreach (var c in changes)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(c.Label).Append('=').Append((useOld ? c.Old : c.New) ?? "");
            }
            return sb.ToString();
        }

        private static string SerializeSnapshot(object entity)
        {
            var type = entity.GetType();
            var sb = new StringBuilder();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (SystemProperties.Contains(prop.Name)) continue;
                if (prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() != null) continue;
                var propAttr = prop.GetCustomAttribute<AuditPropertyAttribute>();
                if (propAttr?.Ignore == true) continue;
                if (!prop.CanRead) continue;

                object? raw;
                try { raw = prop.GetValue(entity); }
                catch { continue; }

                var formatted = FormatValue(raw, propAttr);
                if (formatted == null) continue;

                if (sb.Length > 0) sb.Append("; ");
                sb.Append(prop.Name).Append('=').Append(formatted);
            }
            return sb.ToString();
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value.Substring(0, max);
    }
}
