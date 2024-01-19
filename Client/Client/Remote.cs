using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class RemoteForm : Form
    {
        private Socket clientSocket;
        private MemoryStream memoryStream;
        public RemoteForm(Socket socket)
        {
            InitializeComponent();
            clientSocket = socket;

            // Khởi tạo MemoryStream để lưu trữ dữ liệu hình ảnh từ Server
            memoryStream = new MemoryStream();
        }

        private void RemoteForm_Load(object sender, EventArgs e)
        {
            ClientForm clientForm = new ClientForm();
            clientForm.Close();
            clientSocket.Send(Encoding.ASCII.GetBytes("ReadyForRemoteDesktop"));

            // Bắt đầu lắng nghe và xử lý dữ liệu màn hình từ Server
            Thread receiveThread = new Thread(new ThreadStart(ReceiveScreenData));
            receiveThread.Start();
        }
        private void ReceiveScreenData()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024 * 1024];
                    int bytesRead = clientSocket.Receive(buffer);
                    if (bytesRead == 0)
                        throw new SocketException();
                    if (IsImage(buffer))
                    {
                        using (MemoryStream stream = new MemoryStream(buffer, 0, bytesRead))
                        {
                            // Create a new byte array to store the actual image data
                            byte[] imageData = new byte[bytesRead];
                            Array.Copy(buffer, imageData, bytesRead);

                            // Create an Image from the received byte array
                            using (MemoryStream imageStream = new MemoryStream(imageData))
                            {
                                Image image = Image.FromStream(imageStream);

                                Invoke(new Action(() =>
                                {
                                    pictureBox1.Image = image;
                                    pictureBox1.Invalidate();
                                }));
                            }
                        }
                    }
                        
                }
            }
            catch (SocketException)
            {
                Invoke(new Action(() => MessageBox.Show("Server disconnected")));
                CloseForm();
            }
            catch (Exception ex)
            {
                Invoke(new Action(() => MessageBox.Show("Exception: " + ex.Message)));
                CloseForm();
            }
        }
        private void CloseForm()
        {
            // Đóng form khi xử lý kết thúc
            if (InvokeRequired)
            {
                Invoke(new Action(() => CloseForm()));
            }
            else
            {
                Close();
            }
        }
        private bool IsImage(byte[] buffer)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    Image.FromStream(stream);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void btnCapScreen_Click(object sender, EventArgs e)
        {
            CaptureAndSaveServerScreen();
        }
        private void CaptureAndSaveServerScreen()
        {
            try
            {
                // Check if pictureBox1 has an image
                if (pictureBox1.Image != null)
                {
                    // Convert the image in pictureBox1 to a Bitmap
                    Bitmap serverScreen = new Bitmap(pictureBox1.Image);

                    // Get the current directory of the client application
                    string clientDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    // Specify the directory where you want to save the captured screen within the client's directory
                    string saveDirectory = Path.Combine(clientDirectory, "CapturedScreens");

                    // Create the directory if it doesn't exist
                    if (!Directory.Exists(saveDirectory))
                    {
                        Directory.CreateDirectory(saveDirectory);
                    }

                    // Generate a unique file name (you can customize this logic)
                    string fileName = Path.Combine(saveDirectory, $"CapturedScreen_{DateTime.Now:yyyyMMddHHmmss}.png");

                    // Save the captured screen to the specified file
                    serverScreen.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

                    MessageBox.Show($"Server screen captured and saved to {fileName}");
                }
                else
                {
                    MessageBox.Show("Server screen not available in pictureBox1");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in CaptureAndSaveServerScreen: " + ex.Message);
            }
        }

    }
}
