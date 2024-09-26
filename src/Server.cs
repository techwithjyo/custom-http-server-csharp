using System.IO;
using System.Net;
using System.Net.Sockets;
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

                string directory = GetDirectory();

                if (path == "/user-agent" && userAgent != null)
                {
                    SendResponseWithUserAgent(userAgent, socket);
                }
                else if (path.StartsWith("/echo/"))
                {
                    SendResponseBackWithEchoPath(path, socket);
                }
                else if (path.StartsWith("/files/"))
                {
                    SendResponseBackWithFileContent(path, directory, socket);

                }
                else if (path == "/")
                {
                    SendSimple200Response(socket);
                }
                else
                {
                    Response404NotFound(socket);

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

string GetDirectory()
{
    string[] args = Environment.GetCommandLineArgs();
    string directory = null;
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--directory" && i + 1 < args.Length)
        {
            directory = args[i + 1];
            Console.WriteLine("Directory: " + directory);
            break;
        }
    }
    if (directory == null)
    {
        Console.WriteLine("Directory not specified.");
    }
    return directory;
}

void Response404NotFound(Socket socket)
{
    // Respond with 404 Not Found
    string response = "HTTP/1.1 404 Not Found\r\n\r\n";
    socket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine("Sent response 404 Not Found!");
}

void SendSimple200Response(Socket socket)
{
    // Respond with 200 OK
    string response = "HTTP/1.1 200 OK\r\n\r\n";
    socket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine("Sent response 200 OK!");
}

void SendResponseBackWithFileContent(string path, string directory, Socket socket)
{
    string fileName = path.Substring("/files/".Length);
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

void SendResponseBackWithEchoPath(string path, Socket socket)
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

void SendResponseWithUserAgent(string userAgent, Socket socket)
{
    string response = $"HTTP/1.1 200 OK\r\n" +
                                        "Content-Type: text/plain\r\n" +
                                        $"Content-Length: {userAgent.Length}\r\n\r\n" +
                                        userAgent;
    socket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine("Sent response 200 OK with User Agent!");
}