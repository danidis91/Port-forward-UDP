using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;

namespace PortForward
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        List<UDPClientTask> clientsUDP = new List<UDPClientTask>();

        //Receive UDP packets from selected port and send to selected adress/port
        private async Task UDPListener(string adress, int port, CancellationTokenSource cts)
        {
            UdpClient _udpClient = new UdpClient(port);
            CancellationTokenSource _cts = new CancellationTokenSource();
            clientsUDP.Add(new UDPClientTask { name = adress + ":" + port, udpClient = _udpClient, cts = _cts });
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    UdpReceiveResult receivedResults = await _udpClient.ReceiveAsync();
                    byte[] message = receivedResults.Buffer;
                    SendUDPMessage(message, adress, port);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("UDPClient closed");
            }
        }

        //Send UDP packet from selected adress/port
        public async void SendUDPMessage(byte[] message,string adress, int port)
        {
            UdpClient client = new UdpClient();
            IPAddress broadcast = IPAddress.Parse(adress);
            IPEndPoint endpoint = new IPEndPoint(broadcast, port);
            try
            {
                await client.SendAsync(message, message.Length, endpoint);
                client.Close();
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        //Add a new port forwarding UDP rule
        private void Button_Add(object sender, RoutedEventArgs e)
        {
            bool parse_ok = Int32.TryParse(textBoxPort.Text, out int port);
            if (!parse_ok)
                return;
            foreach (UDPClientTask udpClient in clientsUDP)
            {
                if (udpClient.name == textBoxIP.Text + ":" + textBoxPort.Text)
                    return;
            }

            comboBox.Items.Add(textBoxIP.Text + ":" + textBoxPort.Text);
            comboBox.SelectedItem = comboBox.Items[comboBox.Items.Count -1];
            CancellationTokenSource _cts = new CancellationTokenSource();
            string name = textBoxIP.Text;
            Task taskUDPListener = Task.Run(() => UDPListener(name, port, _cts));
        }

        //Remove an existing port forwarding UDP rule
        private void Button_Remove(object sender, RoutedEventArgs e)
        {
            foreach (UDPClientTask udpClient in  clientsUDP )
            {
                if (udpClient.name == comboBox.SelectedValue.ToString())
                {
                    udpClient.udpClient.Close();
                    udpClient.cts.Cancel();
                    clientsUDP.Remove(udpClient);
                    break;
                }
            }
            comboBox.Items.Remove(comboBox.SelectedValue);
        }

        public class UDPClientTask
        {
            public string name;
            public UdpClient udpClient;
            public CancellationTokenSource cts;
        }
    }
}
