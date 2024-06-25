using System;
using System.Net.Sockets;
using System.Timers;

namespace ClientPolynomialTest
{
    class PolynomialClient
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient(); // Create a new connection
            SessionHandler clientHandler = new SessionHandler();
            clientHandler.startClient(client);
        }
    }

    // handling everything inside an instance means I can share a socket and stream more easily between functions
    public class SessionHandler
    {
        TcpClient clientSocket;
        private static System.Timers.Timer aTimer;
        NetworkStream serverStream;

        public void startClient(TcpClient client)
        {
            this.clientSocket = client;
            Console.WriteLine("Connecting to server...");
            try
            {
                clientSocket.Connect("127.0.0.1", 1234);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
                Environment.Exit(0);
            }
            Console.WriteLine("Connected to server");
            handleRequests();
        }

        void handleRequests()
        {
            while (true)
            {
                String message = "";
                Console.Write("Enter a numeric value for calculation: ");
                message = Console.ReadLine();

                // handle null inputs, I know the server should validate inputs for security reasons, but this is a test project
                if (message != "")
                {
                    byte[] bytes = sendMessage(System.Text.Encoding.Unicode.GetBytes(message));
                    Console.WriteLine(cleanMessage(bytes));
                }
            }
        }

        private byte[] sendMessage(byte[] messageBytes)
        {
            const int bytesize = 1024 * 1024;
            // starting a 3 second timer
            SetTimer(3);
            try
            {
                serverStream = clientSocket.GetStream();
                serverStream.Write(messageBytes, 0, messageBytes.Length); // sends message to server
                // Clean up
                serverStream.Flush();

                messageBytes = new byte[bytesize]; // Clear the message to store the response

                // Receive the server's response
                serverStream.Read(messageBytes, 0, messageBytes.Length);
                serverStream.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
                Environment.Exit(0);
            }
            finally
            {
                aTimer.Stop();
            }
            return messageBytes; // Return response  
        }


        private static string cleanMessage(byte[] bytes)
        {
            string message = System.Text.Encoding.Unicode.GetString(bytes);

            string serverMessage = null;
            foreach (var nchar in message)
            {
                if (nchar != '\0')
                {
                    serverMessage += nchar;
                }
            }
            return serverMessage;
        }

        private static void SetTimer(int seconds)
        {
            // Create a timer that uses seconds parameter
            aTimer = new System.Timers.Timer(seconds * 1000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("The server has not replied in 3 seconds since request");
            // I kinda like having the timer repeat itself on a timeout, it's a little more clear in functionality
            //aTimer.Stop();
        }
    }
}
