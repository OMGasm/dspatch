using dspatch.DS;
using dspatch.IO;
using dspatch.Nitro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace dspatch {
    class Program {
        static byte[] dsHashOriginal =
        {
            0xF1, 0x8B, 0x55, 0xF3, 0xE1, 0x25, 0x9C, 0x03, 0xE1, 0x0D, 0x0E, 0xCB, 0x54, 0x96, 0x93, 0xB4, 0x29, 0x05, 0xCE, 0xB5
        };

        static byte[] dsHashSlimdown =
        {
            0x10, 0x62, 0x86, 0x11, 0x88, 0xA4, 0x54, 0x46, 0x1F, 0xE4, 0x2F, 0x72, 0x3A, 0x4B, 0x51, 0x29, 0xF8, 0x65, 0xE4, 0xD8
        };

        static void PrintUsage()
        {
            Console.WriteLine("Usage: dspatch [-s Station.nds] [-o Patched.nds] {-i ROM1.nds} {-I ROMsFolder1}");
            Console.WriteLine("Note: You can add multiple ROMs by using -I or -i multiple times.");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("== DS Download Station Patcher v1.1 (OctoSpacc fork) ==");
            Console.WriteLine("Exploit by Gericom, shutterbug2000 and Apache Thunder\n");

            if (args.Length <= 1)
            {
                PrintUsage();
                return;
            }

            string dsPath = null;
            string outPath = null;
            List<string> romPaths = new List<string>();
            string filePath;

            //parse arguments
            int q = 0;
            while (q < args.Length - 1)
            {
                string arg = args[q++];
                switch (arg)
                {
                    case "-s":
                        filePath = args[q++];
                        if (!File.Exists(filePath))
                        {
                            Console.WriteLine("Error: File (" + filePath + ") does not exist!");
                            return;
                        }
                        dsPath = filePath;
                        break;
                    case "-o":
                        outPath = args[q++];
                        break;
                    case "-i":
                        filePath = args[q++];
                        if (!File.Exists(filePath))
                        {
                            Console.WriteLine("Error: File (" + filePath + ") does not exist!");
                            return;
                        }
                        romPaths.Add(filePath);
                        break;
                    case "-I":
                        string dirPath = args[q++];
                        if (!Directory.Exists(dirPath))
                        {
                            Console.WriteLine("Error: Directory (" + dirPath + ") does not exist!");
                            return;
                        }
                        romPaths.AddRange(Directory.GetFiles(dirPath, "*.nds"));
                        romPaths.AddRange(Directory.GetFiles(dirPath, "*.srl"));
                        break;
                    default:
                        Console.WriteLine("Error: Invalid argument (" + arg + ")\n");
                        PrintUsage();
                        return;
                }
            }

            if (dsPath == null)
            {
                if (File.Exists("Station.nds"))
                {
                    dsPath = "Station.nds";
                }
                else
                {
                    Console.WriteLine("Error: Specify a download station rom!\n(Station.nds not found in current directory)\n");
                    PrintUsage();
                    return;
                }
            }
            if (outPath == null)
            {
                Console.WriteLine("Outputting to Patched.nds\n");
                outPath = "Patched.nds";
            }
            if (romPaths.Count == 0)
            {
                Console.WriteLine("Error: Specify at least 1 rom!\n");
                PrintUsage();
                return;
            }

            byte[] dsdata = File.ReadAllBytes(dsPath);
            byte[] sha1 = SHA1.Create().ComputeHash(dsdata);
            bool shaWarned = false;

            for (int i = 0; i < 20; i++)
            {
                if ((sha1[i] != dsHashOriginal[i]) && (sha1[i] != dsHashSlimdown[i]) && (!shaWarned))
                {
                    Console.WriteLine("WARNING: Invalid download station rom!");
                    Console.WriteLine("The patcher is only tested with:");
                    Console.WriteLine("- Station.nds, 745 KB (included in this tool's code repository)");
                    Console.WriteLine("  SHA1: 1062861188A454461FE42F723A4B5129F865E4D8");
                    Console.WriteLine("- xxxx - DS Download Station - Volume 1 (Kiosk WiFi Demo Cart) (U)(Independent).nds");
                    Console.WriteLine("  SHA1: F18B55F3E1259C03E10D0ECB549693B42905CEB5");
                    shaWarned = true;
                }
            }
            DownloadStationPatcher p = new DownloadStationPatcher(new NDS(dsdata));
            foreach(var r in romPaths)
                p.AddRom(new NDS(File.ReadAllBytes(r)));
            byte[] finalResult = p.ProduceRom().Write();
            File.Create(outPath).Close();
            File.WriteAllBytes(outPath, finalResult);
        }
    }
}
