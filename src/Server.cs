using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

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

    // Handle the client connection in a new thread
    ThreadPool.QueueUserWorkItem(HandleClient, socket);
}

string GetDirectory()
{
    string directory = null;
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--directory" && i + 1 < args.Length)
        {
            directory = args[i + 1];
            Console.WriteLine("Directory: " + directory);
            return directory;
        }
    }
    return null;
}
void HandleClient(object obj)
{
    var socket = (Socket)obj;

    try
    {
        // Read the request
        using (var networkStream = new NetworkStream(socket))
        using (var reader = new StreamReader(networkStream, Encoding.UTF8))
        {
            string requestLine = reader.ReadLine();
            Console.WriteLine($"Received request: {requestLine}");

            if (requestLine != null)
            {
                string[] requestParts = requestLine.Split(' ');
                string method = requestParts[0];
                string path = requestParts[1];
                Console.WriteLine($"Method: {method}, Path: {path}");

                if (method == "POST" && path.StartsWith("/files/"))
                {
                    // Handle POST request

                    // Read headers
                    int contentLength = 0;
                    string line;
                    while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                    {
                        if (line.StartsWith("Content-Length:"))
                        {
                            contentLength = int.Parse(line.Substring("Content-Length:".Length).Trim());
                            Console.WriteLine($"Content-Length: {contentLength}");
                        }
                    }

                    // Read the request body
                    char[] buffer = new char[contentLength];
                    reader.Read(buffer, 0, contentLength);
                    string requestBody = new string(buffer);
                    Console.WriteLine($"Request Body: {requestBody}");

                    string directory = GetDirectory();
                    //Write to file
                    string fileName = path.Substring("/files/".Length);
                    string filePath = Path.Combine(directory, fileName);
                    Console.WriteLine("FilePath: " + filePath);

                    File.WriteAllText(filePath, requestBody);
                    Console.WriteLine("File created with content!");

                    string response = "HTTP/1.1 201 Created\r\n\r\n";
                    socket.Send(Encoding.UTF8.GetBytes(response));
                    Console.WriteLine("Sent response 201 Created!");
                }
                else
                {
                    if (requestLine != null && requestLine.StartsWith("GET"))
                    {
                        // Extract the path from the request
                        string requestedPath = requestLine.Split(' ')[1];
                        Console.WriteLine($"Requested path: {requestedPath}");

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

                        string[] args = Environment.GetCommandLineArgs();
                        string directory = GetDirectory();

                        if (directory == null)
                        {
                            Console.WriteLine("Directory not specified.");
                        }

                        if (requestedPath == "/user-agent" && userAgent != null)
                        {
                            string response = $"HTTP/1.1 200 OK\r\n" +
                                               "Content-Type: text/plain\r\n" +
                                               $"Content-Length: {userAgent.Length}\r\n\r\n" +
                                               userAgent;
                            socket.Send(Encoding.UTF8.GetBytes(response));
                            Console.WriteLine("Sent response 200 OK with User Agent!");
                        }
                        else if (requestedPath.StartsWith("/echo/"))
                        {
                            string echoPath = requestedPath.Substring("/echo/".Length);
                            Console.WriteLine($"Extracted echo path: {echoPath}");

                            string response = $"HTTP/1.1 200 OK\r\n" +
                                               "Content-Type: text/plain\r\n" +
                                               $"Content-Length: {echoPath.Length}\r\n\r\n" +
                                               echoPath;
                            socket.Send(Encoding.UTF8.GetBytes(response));
                            Console.WriteLine("Sent response 200 OK with echo path!");
                        }
                        else if (requestedPath.StartsWith("/files/"))
                        {
                            string fileName = requestedPath.Substring("/files/".Length);
                            string filePath = Path.Combine(directory, fileName);
                            Console.WriteLine("FilePath: " + filePath);

                            if (File.Exists(filePath))
                            {
                                byte[] fileContent = File.ReadAllBytes(filePath);

                                string response = $"HTTP/1.1 200 OK\r\n" +
                                                   "Content-Type: application/octet-stream\r\n" +
                                                   $"Content-Length: {fileContent.Length}\r\n\r\n";
                                socket.Send(Encoding.UTF8.GetBytes(response));
                                socket.Send(fileContent);
                                Console.WriteLine("Sent response 200 OK with file content!");
                            }
                            else
                            {
                                string response = "HTTP/1.1 404 Not Found\r\n\r\n";
                                socket.Send(Encoding.UTF8.GetBytes(response));
                                Console.WriteLine("Sent response 404 Not Found!");
                            }
                        }
                        else if (requestedPath == "/")
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
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling client: {ex.Message}");
    }
    finally
    {
        socket.Close();
        Console.WriteLine("Client disconnected");
    }


}