using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class ClientForm : Form
    {
        private Socket clientSocket;
        private Thread clientThread;
        private RemoteForm remote;
        public ClientForm()
        {
            InitializeComponent();
            FormClosing += ClientForm_FormClosing;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                clientSocket.Connect(txtIPServer.Text, 12345);
                MessageBox.Show("Connected to server");
                remote = new RemoteForm(clientSocket);
                remote.Show();
                clientThread = new Thread(new ThreadStart(ListenForServer));
                clientThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message);
            }

        }

        private void ListenForServer()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = clientSocket.Receive(buffer);
                    if (bytesRead == 0)
                        throw new SocketException();

                    string messageReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    /*if (!string.IsNullOrEmpty(messageReceived))
                    {
                        UpdateStatus("Received from server: " + messageReceived);
                    }*/
                }
            }
            catch (SocketException)
            {
                MessageBox.Show("Server disconnected");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.Message);
            }
            finally
            {
                if (clientSocket != null && clientSocket.Connected)
                {
                    clientSocket.Close();
                }
            }
        }
        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Close the client socket when the form is closing
            if (clientSocket != null && clientSocket.Connected)
            {
                clientSocket.Close();
            }
        }
    }
}
