
using System.Net;
using System.Net.Sockets;
using Abb.Egm;
using Google.Protobuf;

namespace ConsoleApp2
{
    class Program
    {
        private const int EGM_PORT = 6510;
        private static UdpClient udpClient;
        private static bool isRunning = true;
        private static int messageCount = 0;
        private static DateTime lastDisplayTime = DateTime.MinValue;

        static void Main(string[] args)
        {
            Console.WriteLine("=== ABB EGM TCP Position Reader ===");
            Console.WriteLine("EGM data reading");
            Console.WriteLine("Press Q to exit\n");

            try
            {
                udpClient = new UdpClient(EGM_PORT);
                Console.WriteLine($"Listening {EGM_PORT}...\n");

                // Q key press handler for exit
                Thread keyThread = new Thread(CheckForKeyPress);
                keyThread.IsBackground = true;
                keyThread.Start();

                StartListening();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                Stop();
            }
        }

        private static void StartListening()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, EGM_PORT);

            while (isRunning)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref remoteEndPoint);

                    if (data.Length > 0)
                    {
                        ProcessEGMMessage(data);
                    }
                }
                catch (SocketException ex)
                {
                    if (isRunning)
                    {
                        Console.WriteLine($"Network error: {ex.Message}");
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static void ProcessEGMMessage(byte[] data)
        {
            try
            {
                messageCount++;

                // Show every 0.3 seconds
                if (DateTime.Now - lastDisplayTime < TimeSpan.FromSeconds(0.3))
                    return;

                Console.Clear();

                if (TryParseAsEgmSensor(data))
                {
                    lastDisplayTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{messageCount}] Parsing error: {ex.Message}");
            }
        }

        private static bool TryParseAsEgmSensor(byte[] data)
        {
            try
            {
                var egmSensor = EgmSensor.Parser.ParseFrom(data);
                bool hasData = false;

                Console.WriteLine("=== SENSOR DATA ===");

                // 1. Header information
                if (egmSensor?.Header != null)
                {
                    Console.WriteLine($"Message #{egmSensor.Header.Seqno}, type: {egmSensor.Header.Mtype}, time: {egmSensor.Header.Tm}");
                    hasData = true;
                }

                // 2. Joint positions
                if (egmSensor?.Planned?.Joints != null)
                {
                    var joints = egmSensor.Planned.Joints.Joints;
                    Console.WriteLine("\nJOINT POSITIONS:");
                    for (int i = 0; i < joints.Count; ++i)
                    {
                        Console.WriteLine($"  Axis {i + 1}: {joints[i],8:F2}°");
                    }
                    hasData = true;
                }

                // 3. Cartesian coordinates
                if (egmSensor?.Planned?.Cartesian != null)
                {
                    var cartesian = egmSensor.Planned.Cartesian;

                    double x = cartesian.Pos?.X ?? 0;
                    double y = cartesian.Pos?.Y ?? 0;
                    double z = cartesian.Pos?.Z ?? 0;

                    Console.WriteLine("\nTCP POSITION:");
                    Console.WriteLine($"  X: {x,8:F2} mm");
                    Console.WriteLine($"  Y: {y,8:F2} mm");
                    Console.WriteLine($"  Z: {z,8:F2} mm");

                    // RobotStudio EGM sends U0, U1, U2 like orientation values
                    double rx = cartesian.Orient?.U0 ?? 0; 
                    double ry = cartesian.Orient?.U1 ?? 0;
                    double rz = cartesian.Orient?.U2 ?? 0;

                    Console.WriteLine("\nORIENTATION (Euler):");
                    Console.WriteLine($"  Rx: {rx,7:F2}°");
                    Console.WriteLine($"  Ry: {ry,7:F2}°");
                    Console.WriteLine($"  Rz: {rz,7:F2}°");

                    hasData = true;
                }

                // 4. Speed reference
                if (egmSensor?.SpeedRef?.Joints != null && egmSensor.SpeedRef.Joints.Joints.Count > 0)
                {
                    var speedRef = egmSensor.SpeedRef.Joints.Joints;
                    Console.WriteLine("\nSPEED REFERENCE:");
                    for (int i = 0; i < speedRef.Count; ++i)
                    {
                        Console.WriteLine($"  Axis {i + 1}: {speedRef[i],7:F2} deg/s");
                    }
                    hasData = true;
                }

                return hasData;
            }
            catch (InvalidProtocolBufferException)
            {
                return false;
            }
        }

        private static void CheckForKeyPress()
        {
            while (isRunning)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("\nShutting down...");
                        Stop();
                        break;
                    }
                }
                Thread.Sleep(100);
            }
        }

        private static void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            Console.WriteLine($"\nStopping EGM Reader...");
            Console.WriteLine($"Total messages processed: {messageCount}");

            try
            {
                udpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while closing: {ex.Message}");
            }

            Console.WriteLine("EGM Reader stopped.");
        }
    }
}