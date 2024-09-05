using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Console.WriteLine("Server started on port 4221");

while (true)
{
    // Accept a client socket
    var socket = server.AcceptSocket();
    Console.WriteLine("Client connected");

    // Read the request
    using (var networkStream = new NetworkStream(socket))
    using (var reader = new StreamReader(networkStream, Encoding.UTF8))
    {
        string requestLine = reader.ReadLine();
        Console.WriteLine($"Received request: {requestLine}");

        if (requestLine != null && requestLine.StartsWith("GET"))
        {
            // Extract the path from the request
            string path = requestLine.Split(' ')[1];
            Console.WriteLine($"Requested path: {path}");

            // Read headers
            string userAgent = null;
            string line;
            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            {
                if (line.StartsWith("User-Agent:"))
                {
                    userAgent = line.Substring("User-Agent:".Length).Trim();
                    Console.WriteLine($"Extracted User-Agent: {userAgent}");
                }
            }

            if (path == "/user-agent" && userAgent != null)
            {
                string response = $"HTTP/1.1 200 OK\r\n" +
                                   "Content-Type: text/plain\r\n" +
                                   $"Content-Length: {userAgent.Length}\r\n\r\n" +
                                   userAgent;
                socket.Send(Encoding.UTF8.GetBytes(response)); 
                Console.WriteLine("Sent response 200 OK with User Agent!");
            }
            else if (path.StartsWith("/echo/"))
            {
                string echoPath = path.Substring("/echo/".Length);
                Console.WriteLine($"Extracted echo path: {echoPath}");

                string response = $"HTTP/1.1 200 OK\r\n" +
                                   "Content-Type: text/plain\r\n" +
                                   $"Content-Length: {echoPath.Length}\r\n\r\n" +
                                   echoPath;
                socket.Send(Encoding.UTF8.GetBytes(response));
                Console.WriteLine("Sent response 200 OK with echo path!");
            }
            else if (path == "/")
            {
                // Respond with 200 OK
                string response = "HTTP/1.1 200 OK\r\n\r\n";
                socket.Send(Encoding.UTF8.GetBytes(response));
                Console.WriteLine("Sent response 200 OK!");
            }
            else
            {
                // Respond with 404 Not Found
                string response = "HTTP/1.1 404 Not Found\r\n\r\n";
                socket.Send(Encoding.UTF8.GetBytes(response));
                Console.WriteLine("Sent response 404 Not Found!");
            }
        }
    }
    socket.Close();
    Console.WriteLine("Client disconnected");
}

