using zkemkeeper;

// =============================================================================
//  Prueba de conectividad OPCIÓN A: Standalone SDK (zkemkeeper)
//  Es el SDK correcto para terminales de asistencia ZKTeco (p.ej. MB460 Plus)
//  y el MISMO que usa ConnectZK/ZKBridgeApp en producción.
//
//  Uso:  PullSdkConnTest.exe <ip> [port] [commKey]
//  Ej.:  PullSdkConnTest.exe 192.168.1.201 4370 0
//        PullSdkConnTest.exe 192.168.1.201 4370 123456
//
//  Requisitos (Windows):
//    - Registrar el COM:  regsvr32 zkemkeeper.dll   (consola como administrador)
//    - Compilar/correr en x86.
//  Objetivo: (1) confirmar que CONECTA y (2) que el reloj devuelve checadas.
// =============================================================================

if (args.Length < 1)
{
    Console.Error.WriteLine("Uso: PullSdkConnTest.exe <ip> [port] [commKey]");
    return;
}

string ip = args[0];
int port = args.Length >= 2 ? int.Parse(args[1]) : 4370;
int commKey = args.Length >= 3 ? int.Parse(args[2]) : 0;
const int machineNumber = 1; // número de máquina del SDK (siempre 1 por conexión)

// Instanciar el COM por ProgID (igual que ConnectZK).
Type? typeZkem = Type.GetTypeFromProgID("zkemkeeper.ZKEM");
if (typeZkem is null)
{
    Console.WriteLine("❌ El COM 'zkemkeeper.ZKEM' NO está registrado.");
    Console.WriteLine("   Ejecuta (consola admin):  regsvr32 zkemkeeper.dll");
    return;
}

var zkem = (IZKEM)Activator.CreateInstance(typeZkem, true)!;

// Si el reloj tiene clave de comunicación (Comm Key), hay que fijarla ANTES de conectar.
if (commKey != 0)
{
    zkem.SetCommPassword(commKey);
    Console.WriteLine($"CommKey aplicada: {commKey}");
}

Console.WriteLine($"Conectando -> ip={ip}, port={port}, commKey={(commKey == 0 ? "(sin clave)" : commKey.ToString())}");

if (!zkem.Connect_Net(ip, port))
{
    int err = 0;
    zkem.GetLastError(ref err);
    Console.WriteLine($"❌ NO CONECTÓ. GetLastError = {err}");
    Console.WriteLine("   Pistas: verifica IP/puerto (TCP 4370), red/firewall, y si el reloj tiene CommKey pásala como 3er argumento.");
    return;
}

Console.WriteLine("✅ CONECTÓ correctamente.");

try
{
    // --- Info del dispositivo (confirma que responde, no solo que abre el socket) ---
    string serial = "";
    if (zkem.GetSerialNumber(machineNumber, out serial))
        Console.WriteLine($"   Número de serie: {serial}");

    string firmware = "";
    if (zkem.GetFirmwareVersion(machineNumber, ref firmware))
        Console.WriteLine($"   Firmware: {firmware}");

    // Conteos (cuántos usuarios / huellas / registros tiene el equipo)
    int adminCount = 0, userCount = 0, fpCount = 0, pwdCount = 0,
        oplogCount = 0, attlogCount = 0, faceCount = 0;
    if (zkem.GetDeviceStatus(machineNumber, 2, ref userCount)) Console.WriteLine($"   Usuarios: {userCount}");
    if (zkem.GetDeviceStatus(machineNumber, 6, ref attlogCount)) Console.WriteLine($"   Checadas almacenadas (attlog): {attlogCount}");
    if (zkem.GetDeviceStatus(machineNumber, 21, ref faceCount)) Console.WriteLine($"   Rostros registrados: {faceCount}");
    _ = adminCount; _ = fpCount; _ = pwdCount; _ = oplogCount; // (status ids no usados aquí)

    // --- Leer checadas (General Log) ---
    Console.WriteLine("Leyendo checadas (General Log)...");
    zkem.EnableDevice(machineNumber, false); // congelar el equipo durante la lectura

    if (!zkem.ReadGeneralLogData(machineNumber))
    {
        int err = 0;
        zkem.GetLastError(ref err);
        Console.WriteLine($"⚠️  ReadGeneralLogData falló. GetLastError = {err}");
    }
    else
    {
        int shown = 0, total = 0;
        string enrollNumber;
        int verifyMode, inOutMode, year, month, day, hour, minute, second, workCode = 0;

        while (zkem.SSR_GetGeneralLogData(machineNumber, out enrollNumber, out verifyMode,
                   out inOutMode, out year, out month, out day, out hour, out minute,
                   out second, ref workCode))
        {
            total++;
            if (shown < 10)
            {
                Console.WriteLine($"   #{enrollNumber}  {year:0000}-{month:00}-{day:00} " +
                                  $"{hour:00}:{minute:00}:{second:00}  verify={verifyMode} inout={inOutMode} wc={workCode}");
                shown++;
            }
        }

        Console.WriteLine($"✅ General Log devolvió {total} checada(s). (mostradas las primeras {shown})");
    }

    zkem.EnableDevice(machineNumber, true); // reactivar el equipo
}
finally
{
    zkem.Disconnect();
    Console.WriteLine("Desconectado.");
}
