using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SpaceTime.Core
{
    public class MpegServer
    {
        public MpegServer(int port, Action<Graphics> drawFrame)
        {
            Port = port;
            DrawFrame = drawFrame;
        }

        public int Port { get; set; }
        public Action<Graphics> DrawFrame { get; set; }

        private Thread WThread;

        private void WorkerThread()
        {
            var tcp = new TcpListener(IPAddress.Any, Port);
            tcp.Start();

            ThreadPool.QueueUserWorkItem((x) =>
            {
                while (true)
                {
                    var client = tcp.AcceptTcpClient();

                    using (var ns = client.GetStream())
                    using (var sr = new StreamReader(ns))
                    using (var sw = new StreamWriter(ns))

                    {
                        try
                        {
                            var httpReqBuf = new byte[1024];
                            ns.Read(httpReqBuf, 0, httpReqBuf.Length);

                            sw.WriteLine("HTTP/1.1 200 OK");
                            //  if (Content.Length != 0) sw.WriteLine("Content-Length: " + Content.Length);
                            sw.WriteLine("Content-Type: video/mpeg");
                            sw.WriteLine("Date: " + DateTime.Now.ToUniversalTime().ToString("r"));
                            sw.WriteLine("Server: mws");
                            sw.WriteLine("Connection: keep-alive");
                            sw.WriteLine("");
                            sw.Flush();

                            var frame = new Bitmap(1920, 1080, PixelFormat.Format32bppArgb);

                            var eps = new EncoderParameters();

                            using (var g = Graphics.FromImage(frame))
                            {
                                while (client.Connected)
                                {
                                    DrawFrame(g);

                                    ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);


                                    Encoder myEncoder = Encoder.Quality;
                                    EncoderParameters myEncoderParameters = new EncoderParameters(1);


                                    var myEncoderParameter = new EncoderParameter(myEncoder, 100L);
                                    myEncoderParameters.Param[0] = myEncoderParameter;
                                    frame.Save(ns, jgpEncoder, myEncoderParameters);
                                    ns.Flush();
                                    Thread.Sleep(1000 / 30 /*FPS */);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Console.WriteLine(e);
                        }
                    }
                }
            });
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        public void Start()
        {
            WThread = new Thread(WorkerThread);
            // WThread.Priority = ThreadPriority.AboveNormal;
            // WThread.IsBackground = false;
            WThread.Start();
        }
    }
}
