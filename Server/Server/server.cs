using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class ServerForm : Form
    {
        private Socket serverSocket;
        private Socket clientSocket;
        private Thread listenerThread;

        private BinaryWriter writer;
        private BinaryReader reader;
        private NetworkStream clientStream;
        public ServerForm()
        {
            InitializeComponent();
            FormClosing += ServerForm_FormClosing;
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            UpdateStatus("Listening...");
            listenerThread = new Thread(new ThreadStart(ListenForClients));
            listenerThread.Start();
        }

        private void ListenForClients()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 12345));
            serverSocket.Listen(10);

            try
            {
                while (true)
                {
                    clientSocket = serverSocket.Accept();
                    UpdateStatus("Client connected");

                    Thread clientThread = new Thread(new ThreadStart(HandleClientComm));
                    clientThread.Start();
                }
            }
            catch (SocketException ex)
            {
                UpdateStatus("SocketException: " + ex.Message);
            }
            catch (Exception ex)
            {
                UpdateStatus("Exception: " + ex.Message);
            }
            finally
            {
                if (serverSocket != null && serverSocket.Connected)
                {
                    serverSocket.Close();
                }
            }
        }
       
        private void HandleClientComm()
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                while (true)
                {
                    bytesRead = clientSocket.Receive(buffer);
                    if (bytesRead == 0)
                        throw new SocketException();

                    string messageReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    //UpdateStatus("Received from client: " + messageReceived);

                    // Xử lý yêu cầu từ Client
                    if (messageReceived == "ReadyForRemoteDesktop")
                    {
                        // Nếu Client gửi yêu cầu xem màn hình, gửi dữ liệu màn hình liên tục
                        SendScreenData();
                    }
                    else
                    {
                        // Xử lý các yêu cầu thông thường
                        // Ví dụ: Echo lại thông điệp cho Client
                        byte[] response = Encoding.ASCII.GetBytes("Server: " + messageReceived);
                        clientSocket.Send(response);
                    }
                }
            }
            catch (SocketException)
            {
                UpdateStatus("Client disconnected");
            }
            catch (Exception ex)
            {
                UpdateStatus("Exception: " + ex.Message);
            }
            finally
            {
                if (clientSocket != null && clientSocket.Connected)
                {
                    clientSocket.Close();
                }
            }
        }

        private void SendScreenData()
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                NetworkStream networkStream = new NetworkStream(clientSocket);

                while (true)
                {
                    // Capture the screen
                    Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    using (Graphics g = Graphics.FromImage(screenshot))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
                    }

                    // Convert the bitmap to a byte array
                    byte[] imageData;
                    using (MemoryStream imageStream = new MemoryStream())
                    {
                        screenshot.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                        imageData = imageStream.ToArray();
                    }

                    // Send the raw byte array to the client using NetworkStream
                    networkStream.Write(imageData, 0, imageData.Length);

                    // Temporary pause between sending to reduce CPU load (adjustable)
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {   
                UpdateStatus("Exception in SendScreenData: " + ex.Message);
            }
        }

        private void UpdateStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), status);
                return;
            }

            MessageBox.Show(status);
        }
        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Close the server socket when the form is closing
            if (serverSocket != null && serverSocket.Connected)
            {
                serverSocket.Close();
            }
        }
    }
}
