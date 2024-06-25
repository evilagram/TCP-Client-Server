using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace ServerPolynomialTest
{
    class PolynomialServer
    {
        public static List<SessionHandler> sessionList = new List<SessionHandler>();
        static void Main(string[] args)
        {
            // I added this for fun, to calculate any polynomial you want.
            List<Double> optionalMultiplier = null;
            if (false) // enable this if you want to try it out.
            {
                optionalMultiplier = new List<double>();
                string polyInput = "";
                bool acceptPoly = true;
                while (acceptPoly)
                {
                    Console.WriteLine("Input " + (optionalMultiplier.Count + 1) + "nth polynomial (invalid value cancels): ");
                    polyInput = Console.ReadLine();

                    Double x;
                    if (Double.TryParse(polyInput, out x))
                    {
                        optionalMultiplier.Insert(0,x); //inserts at the front, since the calculation reads back to front
                    }
                    else
                    {
                        if(optionalMultiplier.Count == 0)
                        {
                            optionalMultiplier = null; //nulls this out to avoid inserting an empty array into the session
                        }
                        acceptPoly = false;
                    }
                }
            }

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 1234);
            TcpListener listener = new TcpListener(endpoint);
            TcpClient clientSocket = default(TcpClient);
            listener.Start();
            
            Console.WriteLine("Listening for requests at " + endpoint.Address + ":" + endpoint.Port);

            int counter = 0;
            // Run the loop continuously; this is the server.  
            while (true)
            {
                clientSocket = listener.AcceptTcpClient();
                
                counter += 1;
                Console.WriteLine("Client #" + counter + " has connected!");

                //making a new session object and loading the socket into it, so it can handle everything
                SessionHandler client = new SessionHandler();
                sessionList.Add(new SessionHandler());
                if (optionalMultiplier != null) // if we're doing the custom polynomial thing, then we include it in the start call, otherwise nah
                {
                    sessionList.Last().startClient(clientSocket, Convert.ToString(counter), optionalMultiplier.ToArray());
                }
                else
                {
                    sessionList.Last().startClient(clientSocket, Convert.ToString(counter));
                }
                Console.WriteLine("Number of Sessions: " + sessionList.Count);
            }
        }

        public static void killClient (string KillID)
        {
            sessionList.Remove(sessionList.Find(y => y.clientId == KillID)); //removes the only reference to the client, so GC removes it
            Console.WriteLine("Number of Sessions: " + sessionList.Count);
        }
    }

    public class SessionHandler
    {
        TcpClient clientSocket;
        public string clientId;
        public bool isAlive = true;

        const int bytesize = 1024 * 1024;
        const int sleeptime = 5000;
        static Double[] defaultMultipliers = new Double[] { 8, 1, -0.5 }; //listed from end to beginning

        // This is only operated on by 1 thread at a time, so I don't need to worry about thread safety here
        Dictionary<Double, Double> computationResults = new Dictionary<double, double>();

        public void startClient(TcpClient inClientSocket, string clientId, Double[] multipliers = null)
        {
            this.clientSocket = inClientSocket;
            this.clientId = clientId;
            if(multipliers != null) //lets us set different multipliers for the polynomial if desired
            {
                defaultMultipliers = multipliers;
            }

            // makes a new thread for the client loop
            Thread clientThread = new Thread(handleInput);
            clientThread.Start();
        }

        private void handleInput()
        {
            int requestCount = 0;
            
            string message = null; // client's message
            byte[] buffer = new byte[bytesize];

            while (true) { 
                try
                {
                    requestCount += 1;
                    // gets stream and receives input from the client
                    NetworkStream networkStream = clientSocket.GetStream();
                    networkStream.Read(buffer, 0, (int)clientSocket.ReceiveBufferSize);
                    // cleans up null characters and converts bytes to string 
                    message = cleanMessage(buffer);
                    Console.WriteLine("Client #" + clientId + " Request #" + requestCount);

                    buffer = new byte[bytesize]; // cleared so we don't reuse parts of old inputs
                    // handles the cleaned up result, and replies
                    handleResponseType(message);
                    networkStream.Flush();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    //breaks loop so the thread will die
                    break;
                }
            }
            // deletes instance by removing it from the list of connected clients
            PolynomialServer.killClient(clientId);
        }

        private void handleResponseType(string message)
        {
            //checks if the message is a double, and if it is, assigns parsed result to x
            Double x;
            if (Double.TryParse(message, out x))
            {
                //checks if the double exists in the dictionary already
                if (computationResults.ContainsKey(x))
                {
                    Console.Write("Result Previously Calculated: " + computationResults[x]);
                    sendResponse("Result Previously Calculated: " + computationResults[x]);
                }
                else //else, runs calculation
                {
                    computationResults[x] = recursivePolynomial(x, defaultMultipliers);
                    /* put sleep here to look like it's working on calculating something, and so you can see the difference between
                       a request that takes over and under 3 seconds */
                    Thread.Sleep(sleeptime); //sleeps 5 seconds, so client has a chance to test if 3+ seconds have passed since request
                    sendResponse("Result Calculated: " + computationResults[x]);
                }
            }
            else
            {
                // Sends a default response if the string isn't a double
                sendResponse("Your message, \"" + message + "\" is not numeric. Please enter a numeric value.");
            }
        }

        private void sendResponse(string message)
        {
            // formats our message to bytes for transfer
            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(message);
            NetworkStream networkStream = clientSocket.GetStream();
            // sends it out, then cleans it up
            networkStream.Write(bytes, 0, bytes.Length);
            networkStream.Flush();
        }

        private static string cleanMessage(byte[] bytes)
        {
            string message = System.Text.Encoding.Unicode.GetString(bytes);

            string cleanedMessage = null;
            foreach (var nchar in message)
            {
                if (nchar != '\0')
                {
                    cleanedMessage += nchar;
                }
            }

            Console.WriteLine("Message: " + cleanedMessage);
            return cleanedMessage;
        }
        
        private static Double recursivePolynomial(Double x, Double[] multipliers, Double result = 0)
        // result has a default value, so it shouldn't be included in the initial call, but can be passed along loops
        {
            if (multipliers.Length <= 0) // handles final loop
            {
                return result;
            }

            // adds the exponent of x for this loop times the current multiplier to the result
            result += Math.Pow(x, multipliers.Length - 1) * multipliers.Last();

            Console.WriteLine("exponent: " + (multipliers.Length - 1) + " total: " + result);
            //removes last element of array, letting the array work as our iterator across loops
            Array.Resize(ref multipliers, multipliers.Length - 1);

            return recursivePolynomial(x, multipliers, result); //calls self, keeping track of result across loops
        }
    }
}
