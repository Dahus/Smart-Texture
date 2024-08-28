using System;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.IO;

using System.Runtime.InteropServices;
using System.Threading;
using BuildSoft.VRChat.Osc;
using BuildSoft.VRChat.Osc.Avatar;
using System.Threading.Tasks;
using BuildSoft.VRChat.Osc.Input;
using BuildSoft.VRChat.Osc.Chatbox;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;


//OscParameter.SendAvatarParameter("ActiveAcc1", 1);
//await Task.Delay(1000);
//OscParameter.SendAvatarParameter("ActiveAcc1", 0);


using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using BuildSoft.VRChat.Osc;
using BuildSoft.VRChat.Osc.Avatar;
using System.Drawing.Imaging;

ProgramState currentState = ProgramState.Setup;

string folderName = "FolderForPicture";
string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
float rotationAngle = 0f;
bool isMirrored = false;

if (!Directory.Exists(folderPath))
{
    Directory.CreateDirectory(folderPath);
    Console.WriteLine($"Folder {folderName} was created.");
}

void Setup()
{
    Console.Clear();

    Console.Write("Enter rotation angle (0-360, default 0): ");
    string rotationInput = ReadLineWithEscapeCheck();
    if (rotationInput == null) return;

    if (float.TryParse(rotationInput, out float angle) && angle >= 0 && angle <= 360)
    {
        rotationAngle = angle;
    }
    else
    {
        Console.WriteLine("Default rotation angle is used: 0°.");
    }

    Console.Write("Mirror the image? (y/n, default n): ");
    string mirrorInput = ReadLineWithEscapeCheck();
    if (mirrorInput == null) return;
    isMirrored = mirrorInput.ToLower() == "y";

    ProcessExistingImages(folderPath);
    currentState = ProgramState.Running;
}

void RunProgram()
{
    using (FileSystemWatcher watcher = new FileSystemWatcher())
    {
        watcher.Path = folderPath;
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
        watcher.Filter = "*.*";
        watcher.IncludeSubdirectories = false;
        watcher.EnableRaisingEvents = true;

        watcher.Created += (sender, e) =>
        {
            string extension = Path.GetExtension(e.FullPath).ToLower();
            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                Console.WriteLine($"Image found: {e.Name}");
                ProcessImage(e.FullPath);
            }
        };

        Console.WriteLine("Program started. Press Esc to return to setup, or press Esc again to exit.");

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    currentState = ProgramState.Setup;
                    return;
                }
            }

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"Folder {folderName} is missing, creating again.");
                Directory.CreateDirectory(folderPath);
            }

            Thread.Sleep(500);
        }
    }
}

string ReadLineWithEscapeCheck()
{
    string input = "";
    while (true)
    {
        if (Console.KeyAvailable)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return input;
            }
            if (keyInfo.Key == ConsoleKey.Escape)
            {
                Console.WriteLine("\nProgram exited by user.");
                Environment.Exit(0);
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                {
                    input = input.Substring(0, input.Length - 1);
                    Console.Write("\b \b");
                }
            }
            else
            {
                input += keyInfo.KeyChar;
                Console.Write(keyInfo.KeyChar);
            }
        }
    }
}


while (true)
{
    if (currentState == ProgramState.Setup)
    {
        Setup();
    }
    else if (currentState == ProgramState.Running)
    {
        RunProgram();
    }
}

void ProcessExistingImages(string folderPath)
{
    string[] files = Directory.GetFiles(folderPath, "*.*")
    .Where(file => file.EndsWith(".jpg") || file.EndsWith(".jpeg") || file.EndsWith(".png"))
    .ToArray();

    foreach (var file in files)
    {
        Console.WriteLine($"Existing image found: {Path.GetFileName(file)}");
        ProcessImage(file);
    }
}

void ProcessImage(string imagePath)
{
    try
    {
        using (Bitmap bitmap = new Bitmap(imagePath))
        {
            Bitmap transformedBitmap = TransformImage(bitmap);
            int width = transformedBitmap.Width;
            int height = transformedBitmap.Height;
            float[,] grayscaleArray = new float[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("Image processing cancelled.");
                        return;
                    }

                    Color pixelColor = transformedBitmap.GetPixel(x, y);
                    float grayscaleValue = ConvertToGrayscale(pixelColor);
                    grayscaleArray[y, x] = grayscaleValue;

                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"X:{x} Y:{y}");
                }
            }

            Console.WriteLine("\nImage processed successfully. Sending values to VRChat");

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("\nData output interrupted by user.");
                        return;
                    }

                    float color = grayscaleArray[y, x];
                    OscParameter.SendAvatarParameter("Items/OSCTablet/PixelY", y);
                    OscParameter.SendAvatarParameter("Items/OSCTablet/PixelX", x);
                    OscParameter.SendAvatarParameter("Items/OSCTablet/PixelParam", color);

                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"X:{x} Y:{y} C:{color:F2}    ");

                    System.Threading.Thread.Sleep(200);
                }
            }

            Console.WriteLine("\nData output completed.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing image: {ex.Message}");
    }
}

Bitmap TransformImage(Bitmap originalBitmap)
{
    Bitmap transformedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);

    using (Graphics g = Graphics.FromImage(transformedBitmap))
    {
        g.Clear(Color.Transparent);

        g.TranslateTransform((float)originalBitmap.Width / 2, (float)originalBitmap.Height / 2);

        g.RotateTransform(rotationAngle);

        if (isMirrored)
        {
            g.ScaleTransform(-1, 1);
        }

        g.TranslateTransform(-(float)originalBitmap.Width / 2, -(float)originalBitmap.Height / 2);

        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

        g.DrawImage(originalBitmap, new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height));
    }

    return transformedBitmap;
}

float ConvertToGrayscale(Color color)
{
    return (color.R * 0.3f + color.G * 0.59f + color.B * 0.11f) / 255f;
}

enum ProgramState { Setup, Running }


// typing during 3 second.
//OscChatbox.SetIsTyping(true);
//await Task.Delay(3000);

//OscChatbox.SendMessage("твоя мама ебалась в канаве", direct: true);


//using System.Threading.Tasks;

//string message = "твоя мама ебалась в канаве";
//char[] messageArray = message.ToCharArray();
//int length = messageArray.Length;
//int maxSpacing = 5; 
////for (int into = 0; into < 5; into++) //{
//    //    for (int spaces = 0; spaces <= maxSpacing; spaces++)
//    {
//        string spacedMessage = InsertSpaces(message, spaces);

//        //        OscChatbox.SendMessage(spacedMessage, direct: true);

//        //        await Task.Delay(500);
//    }

//    //    for (int spaces = maxSpacing - 1; spaces >= 0; spaces--)
//    {
//        string spacedMessage = InsertSpaces(message, spaces);

//        //        OscChatbox.SendMessage(spacedMessage, direct: true);

//        //        await Task.Delay(1200);
//    }
//}

////static string InsertSpaces(string text, int spaces)
//{
//    string spaceString = new string(' ', spaces);
//    char[] chars = text.ToCharArray();
//    return string.Join(spaceString, chars);
//}

//using System.Threading.Tasks;

//string message = "тут могла быть ваша реклама ";
//char[] messageArray = message.ToCharArray();
//int length = messageArray.Length;

////for (int into = 0; into < 999; into++)
//{
//    //    for (int i = 0; i < length; i++)
//    {
//        //        string updatedMessage = new string(messageArray.Skip(i).Concat(messageArray.Take(i)).ToArray());

//        //        OscChatbox.SendMessage(updatedMessage, direct: true);

//        //        await Task.Delay(1500);
//    }
//}


//string message = "твоя мама ебалась в канаве";
//char[] messageArray = message.ToCharArray();
//int length = messageArray.Length;


//for (int into = 0; into < 10; into++)
//{

//    for (int i = 0; i < length; i++)
//    {
//        if (messageArray[i] != ' ')
//        {
//            await Task.Delay(1010);
//            //            char[] tempArray = (char[])messageArray.Clone();
//            //            tempArray[i] = char.ToUpper(tempArray[i]);

//            //            for (int j = 0; j < length; j++)
//            {
//                if (j != i && tempArray[j] != ' ')
//                {
//                    tempArray[j] = char.ToLower(tempArray[j]);
//                }
//            }

//            //            string updatedMessage = new string(tempArray);
//            OscChatbox.SendMessage(updatedMessage, direct: true);
//        }
//    }
//}



//OscAxisInput.LookVertical.Send(0.2f);
//await Task.Delay(1000);
//OscAxisInput.LookVertical.Send(0f);


//OscButtonInput.MoveForward.Press();
//await Task.Delay(1000);
//OscButtonInput.MoveForward.Release();





/* Рабочиее хождение

[DllImport("user32.dll")]
static extern short GetAsyncKeyState(int vKey);


Console.WriteLine("Нажмите W, A, S или D для выполнения цикла. Для выхода нажмите Esc.");

while (true)
{
        if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
    {
        break;
    }


    if (IsKeyPressed(ConsoleKey.UpArrow))
    {
        while (IsKeyPressed(ConsoleKey.UpArrow))
        {
            OscButtonInput.MoveForward.Press();
        }
        
    }
    else if (IsKeyPressed(ConsoleKey.LeftArrow))
    {
        while (IsKeyPressed(ConsoleKey.LeftArrow))
        {
            OscButtonInput.MoveLeft.Press();
        }
    }
    else if (IsKeyPressed(ConsoleKey.DownArrow))
    {
        while (IsKeyPressed(ConsoleKey.DownArrow))
        {
            OscButtonInput.MoveBackward.Press();
        }
    }
    else if (IsKeyPressed(ConsoleKey.RightArrow))
    {
        while (IsKeyPressed(ConsoleKey.RightArrow))
        {
            OscButtonInput.MoveRight.Press();
        }
    }
    else if (IsKeyPressed(ConsoleKey.Spacebar))
    {
        while (IsKeyPressed(ConsoleKey.Spacebar))
        {
            OscButtonInput.Jump.Press();
        }
    }

    OscButtonInput.MoveForward.Release();
    OscButtonInput.MoveLeft.Release();
    OscButtonInput.MoveBackward.Release();
    OscButtonInput.MoveRight.Release();
    OscButtonInput.Jump.Release();
}

Console.WriteLine("Программа завершена.");

static bool IsKeyPressed(ConsoleKey key)
{
    int vKey = (int)key;
    return Convert.ToBoolean(GetAsyncKeyState(vKey) & 0x8000);
}

*/

/*

while(input.ToLower() != "stop")
{
    Console.WriteLine("Enter command (to exit, enter 'Stop'):");
    input = Console.ReadLine();

    if(input.ToLower() != "stop")
    {
        Console.WriteLine("You enter: " + input);
    }
}

Console.WriteLine("The program has ended.");
*/

//OscChatbox.SendMessage("some message", direct: true);