using System.Runtime.InteropServices;
using System.Text;

// =============================================================================
//  Prueba de conectividad ZKTeco PULL SDK (plcommpro.dll)
//  Uso:  PullSdkConnTest.exe <ip> <port> [commKey]
//  Ej.:  PullSdkConnTest.exe 192.168.1.201 4370 0
//
//  Requisitos para correr (Windows):
//    - Copiar junto al .exe las 6 DLLs de la carpeta /lib del repo MuaazH:
//        plcommpro.dll, plcomms.dll, plrscagent.dll, plrscomm.dll,
//        pltcpcomm.dll, plusbcomm.dll
//    - Ejecutar como x86.
//  Objetivo: confirmar (1) que CONECTA y (2) que el reloj devuelve checadas
//            en la tabla "transaction".
// =============================================================================

[DllImport("plcommpro.dll", EntryPoint = "Connect")]
static extern IntPtr Connect(string parameters);

[DllImport("plcommpro.dll", EntryPoint = "Disconnect")]
static extern void Disconnect(IntPtr handle);

[DllImport("plcommpro.dll", EntryPoint = "PullLastError")]
static extern int PullLastError();

[DllImport("plcommpro.dll", EntryPoint = "GetDeviceData")]
static extern int GetDeviceData(IntPtr handle, ref byte buffer, int len,
    string table, string fieldNames, string filter, string options);

if (args.Length < 2)
{
    Console.Error.WriteLine("Uso: PullSdkConnTest.exe <ip> <port> [commKey]");
    return;
}

string ip = args[0];
int port = int.Parse(args[1]);
int key = args.Length >= 3 ? int.Parse(args[2]) : 0;
int timeout = 5000;

string connStr = $"protocol=TCP,ipaddress={ip},port={port},timeout={timeout},passwd={(key == 0 ? "" : key.ToString())}";
Console.WriteLine($"Conectando -> {connStr}");

IntPtr handle = Connect(connStr);

if (handle == IntPtr.Zero)
{
    Console.WriteLine($"❌ NO CONECTÓ. PullLastError = {PullLastError()}");
    Console.WriteLine("   (si el error es de auth, prueba con la commKey real del reloj)");
    return;
}

Console.WriteLine("✅ CONECTÓ correctamente.");

try
{
    // Leer la tabla de checadas (transaction). "*" = todos los campos.
    var buffer = new byte[8 * 1024 * 1024];
    int rc = GetDeviceData(handle, ref buffer[0], buffer.Length, "transaction", "*", "", "");

    if (rc < 0)
    {
        Console.WriteLine($"⚠️  Conectó pero GetDeviceData(transaction) falló. rc = {rc}, PullLastError = {PullLastError()}");
    }
    else
    {
        int len = 0;
        while (len < buffer.Length && buffer[len] != 0) len++;
        string txt = Encoding.ASCII.GetString(buffer, 0, len);
        var lines = txt.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        Console.WriteLine($"✅ transaction devolvió {lines.Length} línea(s). Primeras 10:");
        foreach (var line in lines.Take(10))
            Console.WriteLine("   " + line);
    }
}
finally
{
    Disconnect(handle);
    Console.WriteLine("Desconectado.");
}
