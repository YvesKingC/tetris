using System;
using System.Threading;
using System.Diagnostics;

namespace Tetris
{


    class Program
    {
        // Layout
        const int kolom = 10;
        const int baris = 20;
        static char[,] bg = new char[baris, kolom];


        static int score = 0;

        // Variabel 
        const int holdSizeX = 6;
        const int holdSizeY = baris;
        static int holdIndex = -1;
        static char holdChar;

        const int upNextSize = 6;


        static ConsoleKeyInfo input;


        // Posisi Sementara Karakter
        static int PosisiSementaraX = 0;
        static int PosisiSementaraY = 0;
        static char PosisiSementaraChar = 'O';

        static int PosisiSementaraRotasi = 0;



        // Block and Bogs        
        static int[] bag;
        static int[] nextBag;

        static int bagIndex;
        static int IndexSekarang;


        // misc
        static int maxTime = 20;
        static int timer = 0;
        static int amount = 0;


        #region Assets

        readonly static string characters = "OILJSZT";
        readonly static int[,,,] positions =
        {
        {
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}}
        },

        {
        {{2,0},{2,1},{2,2},{2,3}},
        {{0,2},{1,2},{2,2},{3,2}},
        {{1,0},{1,1},{1,2},{1,3}},
        {{0,1},{1,1},{2,1},{3,1}},
        },
        {
        {{1,0},{1,1},{1,2},{2,2}},
        {{1,2},{1,1},{2,1},{3,1}},
        {{1,1},{2,1},{2,2},{2,3}},
        {{2,1},{2,2},{1,2},{0,2}}
        },

        {
        {{2,0},{2,1},{2,2},{1,2}},
        {{1,1},{1,2},{2,2},{3,2}},
        {{2,1},{1,1},{1,2},{1,3}},
        {{0,1},{1,1},{2,1},{2,2}}
        },

        {
        {{2,1},{1,1},{1,2},{0,2}},
        {{1,0},{1,1},{2,1},{2,2}},
        {{2,1},{1,1},{1,2},{0,2}},
        {{1,0},{1,1},{2,1},{2,2}}
        },
        {
        {{0,1},{1,1},{1,2},{2,2}},
        {{1,0},{1,1},{0,1},{0,2}},
        {{0,1},{1,1},{1,2},{2,2}},
        {{1,0},{1,1},{0,1},{0,2}}
        },

        {
        {{0,1},{1,1},{1,0},{2,1}},
        {{1,0},{1,1},{2,1},{1,2}},
        {{0,1},{1,1},{1,2},{2,1}},
        {{1,0},{1,1},{0,1},{1,2}}
        }
        };
        #endregion
        static void Main()
        {
            // Console Cursor Tidak terlihat
            Console.CursorVisible = false;

            // Start the input thread to get live inputs
            Thread inputThread = new Thread(Input);
            inputThread.Start();

            // Generate bag / current block
            bag = GenerateBag();
            nextBag = GenerateBag();
            NewBlock();

            // Background Kosong
            for (int y = 0; y < baris; y++)
                for (int x = 0; x < kolom; x++)
                    bg[y, x] = '-';

            while (true)
            {

                // Force block down
                if (timer >= maxTime)
                {
                    // If it doesn't collide, just move it down. If it does call BlockDownCollision
                    if (!Collision(IndexSekarang, bg, PosisiSementaraX, PosisiSementaraY + 1, PosisiSementaraRotasi)) PosisiSementaraY++;
                    else BlockDownCollision();

                    timer = 0;
                }
                timer++;




                // INPUT
                tombolGerak(); // tombolGerak
                input = new ConsoleKeyInfo(); // Reset input var


                // RENDER CURRENT
                char[,] view = tampilanMapGameBerjalan(); // Render view (Playing field)

                // RENDER HOLD
                char[,] hold = blockYangLagiMain(); // Render hold (the current held block)


                //RENDER UP NEXT
                char[,] next = nextBlockTampil(); // Render the next three blocks as an 'up next' feature

                // PRINT VIEW
                Print(view, hold, next); // Print everything to the screen

                Thread.Sleep(20); // Wait to not overload the processor (I think it's better because it has no impact on game feel)
            }


        }


        static void tombolGerak()
        {
            switch (input.Key)
            {
                // Panah kiri = ke kiri
                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    if (!Collision(IndexSekarang, bg, PosisiSementaraX - 1, PosisiSementaraY, PosisiSementaraRotasi)) PosisiSementaraX -= 1;
                    break;

                // Panah kanan = ke kanan
                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    if (!Collision(IndexSekarang, bg, PosisiSementaraX + 1, PosisiSementaraY, PosisiSementaraRotasi)) PosisiSementaraX += 1;
                    break;

                // Panah atas = rotasi karakter
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    int Rotasi = PosisiSementaraRotasi + 1;
                    if (Rotasi >= 4) Rotasi = 0;
                    if (!Collision(IndexSekarang, bg, PosisiSementaraX, PosisiSementaraY, Rotasi)) PosisiSementaraRotasi = Rotasi;

                    break;

                // Hard drop
                case ConsoleKey.Spacebar:
                    int i = 0;
                    while (true)
                    {
                        i++;
                        if (Collision(IndexSekarang, bg, PosisiSementaraX, PosisiSementaraY + i, PosisiSementaraRotasi))
                        {
                            PosisiSementaraY += i - 1;
                            break;
                        }

                    }
                    score += i + 1;
                    break;

                // Quit
                case ConsoleKey.Escape:
                    Environment.Exit(1);
                    break;

                // Hold block
                case ConsoleKey.Enter:

                    // If there isnt a current held block:
                    if (holdIndex == -1)
                    {
                        holdIndex = IndexSekarang;
                        holdChar = PosisiSementaraChar;
                        NewBlock();
                    }
                    // If there is:
                    else
                    {
                        if (!Collision(holdIndex, bg, PosisiSementaraX, PosisiSementaraY, 0)) // Check for collision
                        {

                            // Switch current and hold
                            int c = IndexSekarang;
                            char ch = PosisiSementaraChar;
                            IndexSekarang = holdIndex;
                            PosisiSementaraChar = holdChar;
                            holdIndex = c;
                            holdChar = ch;
                        }

                    }
                    break;

                // Move down faster
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    timer = maxTime;
                    break;

                case ConsoleKey.R:
                    Restart();
                    break;

                default:
                    break;
            }
        }
        static void BlockDownCollision()
        {

            // Add blocks from current to background
            for (int i = 0; i < positions.GetLength(2); i++)
            {
                bg[positions[IndexSekarang, PosisiSementaraRotasi, i, 1] + PosisiSementaraY, positions[IndexSekarang, PosisiSementaraRotasi, i, 0] + PosisiSementaraX] = PosisiSementaraChar;
            }

            // Loop 
            while (true)
            {
                // Check for line
                int lineY = Line(bg);

                // If a line is detected
                if (lineY != -1)
                {
                    ClearLine(lineY);

                    continue;
                }
                break;
            }
            // New block
            NewBlock();

        }


        static void Restart()
        {
            // Quite messy but it kinda works. Code by: KeremEskicinar
            var applicationPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Process.Start(applicationPath);
            Environment.Exit(Environment.ExitCode);
        }

        static void ClearLine(int lineY)
        {
            score += 40;
            // Clear said line
            for (int x = 0; x < kolom; x++) bg[lineY, x] = '-';

            // Loop through all blocks above line
            for (int y = lineY - 1; y > 0; y--)
            {
                for (int x = 0; x < kolom; x++)
                {
                    // Move each character down
                    char character = bg[y, x];
                    if (character != '-')
                    {
                        bg[y, x] = '-';
                        bg[y + 1, x] = character;
                    }

                }
            }
        }

        static char[,] tampilanMapGameBerjalan()
        {
            char[,] view = new char[baris, kolom];

            // Make view equal to bg
            for (int y = 0; y < baris; y++)
                for (int x = 0; x < kolom; x++)
                    view[y, x] = bg[y, x];



            // Overlay current
            for (int i = 0; i < positions.GetLength(2); i++)
            {
                view[positions[IndexSekarang, PosisiSementaraRotasi, i, 1] + PosisiSementaraY, positions[IndexSekarang, PosisiSementaraRotasi, i, 0] + PosisiSementaraX] = PosisiSementaraChar;
            }
            return view;
        }

        static char[,] blockYangLagiMain()
        {
            char[,] hold = new char[holdSizeY, holdSizeX];
            // Hold = ' ' array
            for (int y = 0; y < holdSizeY; y++)
                for (int x = 0; x < holdSizeX; x++)
                    hold[y, x] = ' ';


            // If there is a held block
            if (holdIndex != -1)
            {
                // Overlay blocks from hold
                for (int i = 0; i < positions.GetLength(2); i++)
                {
                    hold[positions[holdIndex, 0, i, 1] + 1, positions[holdIndex, 0, i, 0] + 1] = holdChar;
                }
            }
            return hold;
        }
        static char[,] nextBlockTampil()
        {
            // Up next = ' ' array   
            char[,] next = new char[baris, upNextSize];
            for (int y = 0; y < baris; y++)
                for (int x = 0; x < upNextSize; x++)
                    next[y, x] = ' ';


            int nextBagIndex = 0;
            for (int i = 0; i < 3; i++) // Next 3 blocks
            {

                for (int l = 0; l < positions.GetLength(2); l++)
                {
                    if (i + bagIndex >= 7) // If we need to acces the next bag
                        next[positions[nextBag[nextBagIndex], 0, l, 1] + 5 * i, positions[nextBag[nextBagIndex], 0, l, 0] + 1] = characters[nextBag[nextBagIndex]];
                    else
                        next[positions[bag[bagIndex + i], 0, l, 1] + 5 * i, positions[bag[bagIndex + i], 0, l, 0] + 1] = characters[bag[bagIndex + i]];


                }
                if (i + bagIndex >= 7) nextBagIndex++;
            }
            return next;

        }

        static void Print(char[,] view, char[,] hold, char[,] next)
        {
            for (int y = 0; y < baris; y++)
            {

                for (int x = 0; x < holdSizeX + kolom + upNextSize; x++)
                {
                    char i = ' ';
                    // Add hold + Main View + up next to view (basically dark magic)
                    if (x < holdSizeX) i = hold[y, x];
                    else if (x >= holdSizeX + kolom) i = next[y, x - kolom - upNextSize];
                    else i = view[y, (x - holdSizeX)];


                    // Colours
                    switch (i)
                    {
                        case 'O':
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(i);
                            break;
                        case 'I':
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write(i);
                            break;

                        case 'T':
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(i);
                            break;

                        case 'S':
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.Write(i);
                            break;
                        case 'Z':
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.Write(i);
                            break;
                        case 'L':
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write(i);
                            break;

                        case 'J':
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.Write(i);
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write(i);
                            break;
                    }

                }
                if (y == 1)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("   " + score);
                }
                Console.WriteLine();
            }

            // Reset console cursor position
            Console.SetCursorPosition(0, Console.CursorTop - baris);
        }
        static int[] GenerateBag()
        {
            Random random = new Random();
            int n = 7;
            int[] ret = { 0, 1, 2, 3, 4, 5, 6, 7 };
            while (n > 1)
            {
                int k = random.Next(n--);
                int temp = ret[n];
                ret[n] = ret[k];
                ret[k] = temp;

            }
            return ret;

        }
        static bool Collision(int index, char[,] bg, int x, int y, int rot)
        {

            for (int i = 0; i < positions.GetLength(2); i++)
            {
                // Check if out of bounds
                if (positions[index, rot, i, 1] + y >= baris || positions[index, rot, i, 0] + x < 0 || positions[index, rot, i, 0] + x >= kolom)
                {
                    return true;
                }
                // Check if not '-'
                if (bg[positions[index, rot, i, 1] + y, positions[index, rot, i, 0] + x] != '-')
                {
                    return true;
                }
            }

            return false;
        }

        static int Line(char[,] bg)
        {
            for (int y = 0; y < baris; y++)
            {
                bool i = true;
                for (int x = 0; x < kolom; x++)
                {
                    if (bg[y, x] == '-')
                    {
                        i = false;
                    }
                }
                if (i) return y;
            }

            // If no line return -1
            return -1;
        }

        static void NewBlock()
        {
            // Check if new bag is necessary
            if (bagIndex >= 7)
            {
                bagIndex = 0;
                bag = nextBag;
                nextBag = GenerateBag();
            }

            // Reset everything
            PosisiSementaraY = 0;
            PosisiSementaraX = 4;
            PosisiSementaraChar = characters[bag[bagIndex]];
            IndexSekarang = bag[bagIndex];

            // Check if the next block position collides. If it does its gameover
            if (Collision(IndexSekarang, bg, PosisiSementaraX, PosisiSementaraY, PosisiSementaraRotasi) && amount > 0)
            {
                GameOver();
            }
            bagIndex++;
            amount++;
        }


        static void GameOver()
        {
            // Possible restart functionality
            Environment.Exit(1);
        }
        static void Input()
        {
            while (true)
            {
                // Get input
                input = Console.ReadKey(true);
            }
        }
    }
}


