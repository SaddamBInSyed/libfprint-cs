using System;
using System.Threading;

namespace Unosquare.Labs.LibFprint.Tests
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            // The device manager discovers devices. It's a singleton and is used to detect connected devices
            // it also create references to the fingerprint scanners
            using (var manager = FingerprintDeviceManager.Instance)
            {
                // We always need to call this method to make sure the library is initialized
                manager.Initialize();
                Console.WriteLine("Initialized Device Manager.");

                // Now we call the device discovery method
                var devices = manager.DiscoverDevices();

                // Let's do stuff with each of the discovered devices (typically only 1)
                foreach (var device in devices)
                {
                    var thread = new Thread(() =>
                    {

                        // Before we do anything, we need to open the device.
                        device.Open();

                        // Now we print some info about the device.
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine();
                        Console.WriteLine("Device {0} - {1}", device.DriverName, device.DriverFullName);
                        Console.WriteLine("    Enroll Stages:      {0}", device.EnrollStagesCount);
                        Console.WriteLine("    Supports Imaging:   {0}", device.SupportsImaging);
                        Console.WriteLine("    Supports Ident:     {0}", device.SupportsIdentification);
                        Console.WriteLine("    Imaging Dimensions: {0}x{1}", device.ImageWidth, device.ImageHeight);

                        // We will enroll a few fingerprints into the gallery.
                        using (var gallery = new FingerprintGallery())
                        {
                            var enrollCount = 0;
                            while (enrollCount < 5)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine(" >> ENROLL: Enroll count: {0}. Enroll a new finger now . . .", enrollCount);

                                // Call the enrollment method
                                var enrollResult = device.EnrollFingerprint("enroll.pgm");
                                if (enrollResult.IsEnrollComplete)
                                {

                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine(" >> VERIFY: Now, verify your scan just to make sure . . .");

                                    // Although not necessary, we are adding verification just to make sure
                                    var isVerified = device.VerifyFingerprint(enrollResult, "verify.pgm");
                                    if (isVerified)
                                    {
                                        enrollCount++;
                                        var printName = "The print " + enrollCount;
                                        gallery.Add(printName, enrollResult);
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Could not verify. Try again!");
                                        Console.WriteLine();
                                    }
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Try Again -- Error Code {0} - {1}", enrollResult.ResultCode, enrollResult.Result);
                                    Console.WriteLine();
                                    // HACK: for some reason we needed the Reset method to be called. Otherwise the reader would blink rapidly and get stuck
                                    device.Reset();
                                }
                            }

                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine();

                            // Now, let's try some identification in the gallery we created earlier
                            // with enrollment and verification operations
                            while (true)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine(" >> IDENTIFY: Press finger against scanner to identify . . .");

                                // Let's try to identify a fingerprint and getting it's key back.
                                // a null key means the FP was not identified.
                                var identified = device.IdentifyFingerprint(gallery, "identify.pgm");
                                if (identified == null)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Could not identify.");
                                    Console.WriteLine();
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Blue;
                                    Console.WriteLine("Fingerprint was identified: {0}.", identified);
                                    Console.WriteLine();
                                }
                            }
                        }
                    })
                    { IsBackground = true };


                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Press A to abort the thread . . .");

                    thread.Start();

                    while (true)
                    {
                        if (Console.ReadKey(true).Key == ConsoleKey.A)
                        {
                            thread.Abort();
                            var terminationTimeout = DateTime.Now.AddSeconds(10);
                            while (thread.IsAlive)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine("Waiting for thread termination. {0.00} seconds to terminate forcefully.", terminationTimeout.Subtract(DateTime.Now).TotalSeconds);
                                thread.Abort();

                                if (DateTime.Now > terminationTimeout)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Termination timeout reached. Forcefully disposing the device.");
                                }

                                Thread.Sleep(1000);
                            }

                            break;
                        }

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Press A to abort the thread . . .");
                    }


                    // We realease unmanaged resources for the device.
                    device.Dispose();
                }

            }


        }
    }



}