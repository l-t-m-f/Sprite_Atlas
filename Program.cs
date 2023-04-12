using System.Runtime.InteropServices;
using static SDL2.SDL;
using static SDL2.SDL_image;

namespace SpriteAtlas
{
internal class Program
    {
        private const string DIRECTORY_PATH = "Images";
        private const int WINDOW_W = 1280;
        private const int WINDOW_H = 720;
        private static IntPtr _window;
        internal static IntPtr Renderer;

        static void Main(string[] args)
        {
            Setup();
            Atlas atlas = new();
            const string search_pattern = "*.png";
            var file_names = Directory.GetFiles(DIRECTORY_PATH, search_pattern);

            var c = 0;

            foreach (var file_name in file_names)
            {
                var file_path = Path.GetRelativePath(".", file_name);
                //Console.WriteLine(filePath);
                atlas.SurfaceData[c] = IMG_Load(file_path);
                atlas.Entries[c] = new AtlasEntry(file_path, new SDL_Rect { x = 0, y = 0, w = 0, h = 0 }, false);
                c++;
            }

            Array.Sort(atlas.SurfaceData, atlas);

            for (var i = 0 ; i < c ; i++)
            {
                SDL_QueryTexture(SDL_CreateTextureFromSurface(Renderer, atlas.SurfaceData[i]), out _, out _, out int w, out int h);

                var rotated = false;
                var found_node = Atlas.FindNode(atlas.First, w, h);

                if (found_node == null)
                {
                    rotated = true;
                    found_node = Atlas.FindNode(atlas.First, h, w);
                }

                if (found_node != null)
                {
                    Console.WriteLine($"Node found for image #{i}");

                    //int rotations = 0;

                    if(rotated)
                    {
                        found_node.Height = w;
                        found_node.Width = h;
                        //rotations++;
                    }

                    var dest = new SDL_Rect {
                        x = found_node.X,
                        y = found_node.Y,
                        w = w,
                        h = h
                    };

                    atlas.Entries[i]!.Rectangle = dest;
                    atlas.Entries[i]!.Rotated = rotated;

                    if (rotated == false)
                    {
                        SDL_BlitSurface(atlas.SurfaceData[i], 
                            IntPtr.Zero, 
                            atlas.MasterSurface, 
                            ref dest);
                    }
                    else
                    {
                        BlitRotated(atlas.SurfaceData[i], 
                            atlas.MasterSurface, 
                            dest.x, dest.y);
                    }
                }
            }

            var master_texture = SDL_CreateTextureFromSurface(Renderer, atlas.MasterSurface);
            var test_extract = atlas.GetAtlasImage("Images\\AnimFrame1.png");
            SDL_QueryTexture(test_extract, out _, out _, 
                out var extract_w,
                out var extract_h);

            var dst_rect = new SDL_Rect { x = 0, y = 0, w = extract_w, h = extract_h };
            var dst_rect2 = new SDL_Rect { x = 0, y = 0, w = Atlas.ATLAS_SIZE, h = Atlas.ATLAS_SIZE };

            foreach (var ae in atlas.Entries)
            {
                if (ae is null) break;
                else
                    Console.WriteLine(ae.Filename);
            }

            while (true)
            {
                while ((SDL_PollEvent(out SDL_Event e)) != 0)
                {
                    switch (e.type)
                    {
                        case SDL_EventType.SDL_QUIT:
                            Environment.Exit(0);
                            break;
                        default:
                            break;
                    }
                }

                SDL_SetRenderDrawColor(Renderer, 255, 0, 0, 255);
                SDL_RenderClear(Renderer);
                SDL_RenderCopy(Renderer,
                    test_extract,
                    0, ref dst_rect);
                SDL_RenderPresent(Renderer);
            }
            
        }
        
        private static void Setup()
        {
            if (SDL_Init(SDL_INIT_VIDEO) < 0)
            {
                Console.WriteLine(
                    $"There was an issue starting SDL:\n{SDL_GetError()}!");
            }

            _window = SDL_CreateWindow("SpriteAtlas Test",
                SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED,
                WINDOW_W, WINDOW_H, SDL_WindowFlags.SDL_WINDOW_SHOWN);

            if (_window == IntPtr.Zero)
            {
                Console.WriteLine(
                    $"There was an issue creating the window:\n{SDL_GetError()}");
            }

            Renderer = SDL_CreateRenderer(_window, -1,
                SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

            if (Renderer == IntPtr.Zero)
            {
                Console.WriteLine(
                    $"There was an issue creating the renderer:\n{SDL_GetError()}");
            }

            IMG_InitFlags img_flags = IMG_InitFlags.IMG_INIT_PNG;
            if (IMG_Init(img_flags) != (int)img_flags)
            {
                Console.WriteLine(
                    $"There was an issue starting SDL_image:\n{SDL_GetError()}!");
            }
        }

        private static void BlitRotated(nint srcPtr, nint destPtr, int destX, int destY)
        {
            var src = Marshal.PtrToStructure<SDL_Surface>(srcPtr);
            //var dest = Marshal.PtrToStructure<SDL_Surface>(destPtr);
            int x;

            var dy = 0;

            for (x = 0 ; x < src.w ; x++)
            {
                var dx = src.h - 1;

                int y;
                for (y = 0 ; y < src.h ; y++)
                {
                    var p = GetPixel(srcPtr, x, y); // Change the type of p to uint
                    PutPixel(destPtr, destX + dx, destY + dy, p); // Pass destPtr instead of dest

                    dx--;
                }

                dy++;
            }
        }


        private static uint GetPixel(nint surfacePtr, int x, int y)
        {
            var surface = Marshal.PtrToStructure<SDL_Surface>(surfacePtr);
            var format = Marshal.PtrToStructure<SDL_PixelFormat>(surface.format);
            var pixels = surface.pixels;
            int bpp = format.BytesPerPixel;
            var p = new IntPtr(pixels.ToInt64() + y * surface.pitch + x * bpp);
            uint pixel = 0;

            switch (bpp)
            {
                case 1:
                    pixel = Marshal.ReadByte(p);
                    break;
                case 2:
                    pixel = (uint)Marshal.ReadInt16(p);
                    break;
                case 3:
                    if (BitConverter.IsLittleEndian)
                    {
                        pixel = (uint)(Marshal.ReadByte(p) |
                                      Marshal.ReadByte(new IntPtr(p.ToInt64() + 1)) << 8 |
                                      Marshal.ReadByte(new IntPtr(p.ToInt64() + 2)) << 16);
                    }
                    else
                    {
                        pixel = (uint)(Marshal.ReadByte(new IntPtr(p.ToInt64() + 2)) |
                                      Marshal.ReadByte(new IntPtr(p.ToInt64() + 1)) << 8 |
                                      Marshal.ReadByte(p) << 16);
                    }
                    break;
                case 4:
                    pixel = (uint)Marshal.ReadInt32(p);
                    break;
            }

            return pixel;
        }


        private static void PutPixel(nint surfacePtr, int x, int y, uint pixel)
        {
            var surface = Marshal.PtrToStructure<SDL_Surface>(surfacePtr);
            var format = Marshal.PtrToStructure<SDL_PixelFormat>(surface.format);
            var pixels = surface.pixels;
            int bpp = format.BytesPerPixel;
            var p = new IntPtr(pixels.ToInt64() + y * surface.pitch + x * bpp);

            switch (bpp)
            {
                case 1:
                    Marshal.WriteByte(p, (byte)pixel);
                    break;
                case 2:
                    Marshal.WriteInt16(p, (short)pixel);
                    break;
                case 3:
                    if (BitConverter.IsLittleEndian)
                    {
                        Marshal.WriteByte(p, (byte)pixel);
                        Marshal.WriteByte(new IntPtr(p.ToInt64() + 1), (byte)(pixel >> 8));
                        Marshal.WriteByte(new IntPtr(p.ToInt64() + 2), (byte)(pixel >> 16));
                    }
                    else
                    {
                        Marshal.WriteByte(p, (byte)(pixel >> 16));
                        Marshal.WriteByte(new IntPtr(p.ToInt64() + 1), (byte)(pixel >> 8));
                        Marshal.WriteByte(new IntPtr(p.ToInt64() + 2), (byte)pixel);
                    }
                    break;
                case 4:
                    Marshal.WriteInt32(p, (int)pixel);
                    break;
            }
        }

    }
}