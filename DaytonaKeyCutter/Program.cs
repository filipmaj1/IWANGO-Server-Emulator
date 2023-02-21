using System;
using System.IO;
using System.Text;

namespace IWANGOEmulator.DaytonaKeyCutter
{
    class Program
    {
        private const string KEY_FILENAME = "DAYTKEY_";
        private static readonly SegaCrypto Crypto = new SegaCrypto(Encoding.ASCII.GetBytes("iloveosamu27"));

        // Generates Daytona US key files to allow users to login to the game.
        // Encrypt Mode: keycutter <username> [-p password] [-ip password] [-out filename]
        // Decrypt Mode: keycutter -d <path>
        static void Main(string[] args)
        {
            Console.WriteLine("Daytona USA KeyCutter by Ioncannon... creates online key files for the Dreamcast game");

            if (args.Length >= 1)
            {
                string username = args[0];
                string filepath = "./";
                string ip = "192.168.0.249";

                // Set Args
                for (int i = 0; i < args.Length; i++)
                {
                    // Decrypt Mode
                    if ((args[i].Equals("-d", StringComparison.OrdinalIgnoreCase) || args[i].Equals("-decrypt", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                    {
                        string path = args[++i];
                        LoadAndDecrypt(path);
                        return;
                    }
                    // Encrypt Mode
                    else if ((args[i].Equals("-u", StringComparison.OrdinalIgnoreCase) || args[i].Equals("-username", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                        username = args[++i];
                    else if ((args[i].Equals("-o", StringComparison.OrdinalIgnoreCase) || args[i].Equals("-out", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                        filepath = args[++i];
                    else if(args[i].Equals("-ip", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                        ip = args[++i];
                    else
                    {
                        Console.WriteLine($"Unknown argument: {args[i]}");
                        return;
                    }
                }

                // Starting cutting that key!
                Console.WriteLine("Cutting you a new key...");
                byte[] encryptedKey = CutKey(username, ip);
                byte[] vmsFile = GenerateVMS(encryptedKey);
                byte[] vmiFile = GenerateVMI(vmsFile.Length);

                File.WriteAllBytes($"{filepath}/{KEY_FILENAME}.VMS", vmsFile);
                File.WriteAllBytes($"{filepath}/{KEY_FILENAME}.VMI", vmiFile);

                Console.WriteLine($"{KEY_FILENAME}.VMS and {KEY_FILENAME}.VMI generated.");
            }
            else
            {
                ShowHelp();
            }            
        }

        private static void LoadAndDecrypt(string path)
        {
            if (File.Exists(path))
            {
                byte[] vms = File.ReadAllBytes(path);
                if (vms.Length == 0x800)
                {
                    string signature = Encoding.ASCII.GetString(vms, 0, 0x10);
                    if (signature.Equals("KEY DATA        "))
                    {
                        byte[] keyData = new byte[0x50];
                        Array.Copy(vms, 0x680, keyData, 0, 0x50);
                        Crypto.Decrypt(keyData);
                        string username = Encoding.ASCII.GetString(keyData, 0, 0x20);
                        string ip = Encoding.ASCII.GetString(keyData, 0x20, 0x10);
                        username = username.Remove(username.IndexOf('\0'));
                        ip = ip.Remove(ip.IndexOf('\0'));
                        Console.WriteLine($"Loaded and decrypted key!\n->Username: {username}\n->IP: {ip}");
                        return;
                    }
                }
                Console.WriteLine("Not a valid Daytona Key VMS file.");
            }
            else
                Console.WriteLine("Please point to valid VMS file.");
        }

        public static byte[] CutKey(string username, string ip)
        {
            byte[] data = new byte[0x50];
            byte[] usernameBytes = Encoding.ASCII.GetBytes(username);
            byte[] ipBytes = Encoding.ASCII.GetBytes(ip);
            Array.Copy(usernameBytes, data, Math.Min(usernameBytes.Length, 0x1f));
            Array.Copy(ipBytes, 0, data, 0x20, Math.Min(ip.Length, 0x0f));
            Crypto.Encrypt(data, 0x30);
            return data;
        }

        public static byte[] GenerateVMI(int fileSize)
        {
            byte[] vmiFile = new byte[0x6C];

            uint checksum = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(KEY_FILENAME.Substring(0,4))) & 0x41474553; // AND first 4 filename bytes with SEGA
            DateTime currentDate = DateTime.Now;

            using (MemoryStream stream = new MemoryStream(vmiFile))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(checksum);
                writer.BaseStream.Seek(0x04, SeekOrigin.Begin);
                writer.Write(Encoding.ASCII.GetBytes("Daytona Key File"));
                writer.BaseStream.Seek(0x24, SeekOrigin.Begin);
                writer.Write(Encoding.ASCII.GetBytes("IONCANNON IONCANNON IONCANNON"));
                writer.BaseStream.Seek(0x44, SeekOrigin.Begin);
                writer.Write((UInt16)currentDate.Year);
                writer.Write((Byte)currentDate.Month);
                writer.Write((Byte)currentDate.Day);
                writer.Write((Byte)currentDate.Hour);
                writer.Write((Byte)currentDate.Minute);
                writer.Write((Byte)currentDate.Second);
                writer.Write((Byte)currentDate.DayOfWeek);
                writer.Write((UInt16)0);
                writer.Write((UInt16)1);
                writer.BaseStream.Seek(0x50, SeekOrigin.Begin);
                writer.Write(Encoding.ASCII.GetBytes($"{KEY_FILENAME}"));
                writer.BaseStream.Seek(0x58, SeekOrigin.Begin);
                writer.Write(Encoding.ASCII.GetBytes($"DAYTONA__KEY"));
                writer.BaseStream.Seek(0x64, SeekOrigin.Begin);
                writer.Write((UInt16)0);
                writer.Write((UInt16)0);
                writer.Write((UInt32)fileSize);
            }

            return vmiFile;
        }

        public static byte[] GenerateVMS(byte[] keyData)
        {
            byte[] fileOut = new byte[0x800];
            Array.Copy(DaytonaKeyVMS.VMSHeader, fileOut, DaytonaKeyVMS.VMSHeader.Length);
            Array.Copy(keyData, 0, fileOut, 0x680, 0x50);
            Array.Copy(DaytonaKeyVMS.VMSFooter, 0, fileOut, 0x680 + 0x50, DaytonaKeyVMS.VMSFooter.Length);
            return fileOut;
        }

        public static void ShowHelp()
        {
            Console.WriteLine("How to use:\nEncrypt Mode: keycutter <username> [-p password] [-ip password] [-out filename]\nDecrypt Mode: keycutter -d <path>");
        }
    }
}
