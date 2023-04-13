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
                var file_names =
                    Directory.GetFiles(DIRECTORY_PATH, search_pattern);

                foreach (var file_name in file_names)
                    {
                        var file_path = Path.GetRelativePath(".", file_name);
                        //Console.WriteLine(filePath);
                        atlas.SurfaceData[atlas.Entries.Count] = IMG_Load(file_path);
                        atlas.Entries.Add(new AtlasEntry(file_path, new SDL_Rect { x = 0, y = 0, w = 0, h = 0 }, false));

                    }

                Array.Resize(ref atlas.SurfaceData, atlas.Entries.Count);
                Array.Sort(atlas.SurfaceData, atlas);

                for (var i = 0; i < atlas.SurfaceData.Length; i++)
                    {
                        SDL_QueryTexture(
                            SDL_CreateTextureFromSurface(Renderer,
                                atlas.SurfaceData[i]), out _, out _, out int w,
                            out int h);

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
                                
                                var dest = new SDL_Rect
                                    {
                                        x = found_node.X,
                                        y = found_node.Y,
                                        w = found_node.Width,
                                        h = found_node.Height
                                    };

                                atlas.Entries[i] = new AtlasEntry(atlas.Entries[i].Filename, dest, rotated);

                                if (rotated == false)
                                    {
                                        SDL_BlitSurface(atlas.SurfaceData[i],
                                            IntPtr.Zero,
                                            atlas.MasterSurface,
                                            ref dest);
                                    }
                                else
                                    {
                                        IntPtr result = BlitRotated(atlas.SurfaceData[i]);
                                        if (result != IntPtr.Zero)
                                            {
                                                
                                                SDL_BlitSurface(result,
                                                IntPtr.Zero,
                                                atlas.MasterSurface,
                                                    ref dest);
                                            }
                                    }
                            }
                        SDL_FreeSurface(atlas.SurfaceData[i]);
                    }

                var master_texture =
                    SDL_CreateTextureFromSurface(Renderer, atlas.MasterSurface);
                var test_extract =
                    atlas.GetAtlasImage("Images\\Tear.png");
                SDL_QueryTexture(test_extract, out _, out _,
                    out var extract_w,
                    out var extract_h);

                var dst_rect = new SDL_Rect
                        { x = 50, y = 50, w = extract_w, h = extract_h };
                var dst_rect2 = new SDL_Rect
                    {
                        x = 0, y = 0, w = Atlas.ATLAS_SIZE, h = Atlas.ATLAS_SIZE
                    };

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
                            master_texture,
                            0, ref dst_rect2);
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
                    SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                    SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

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
        
        private static (byte r, byte g, byte b, byte a) GetPixelColorValues(IntPtr surface_ptr, int x, int y)
            {
                uint pixel = GetPixel(surface_ptr, x, y);
                byte r = (byte)((pixel >> 24) & 0xFF);
                byte g = (byte)((pixel >> 16) & 0xFF);
                byte b = (byte)((pixel >> 8) & 0xFF);
                byte a = (byte)(pixel & 0xFF);
                return (r, g, b, a);
            }


        public static IntPtr BlitRotated(IntPtr source_surface_ptr)
            {
                SDL_Surface source_surface =
                    Marshal.PtrToStructure<SDL_Surface>(source_surface_ptr);
                
                Console.WriteLine($"Source surface dimensions: {source_surface.w}x{source_surface.h}");

                
                IntPtr dest_surface_ptr = SDL_CreateRGBSurfaceWithFormat(0,
                    source_surface.h, source_surface.w, 32,
                    SDL_PIXELFORMAT_ARGB8888);

                SDL_LockSurface(source_surface_ptr);
                SDL_LockSurface(dest_surface_ptr);

                for (int y = 0; y < source_surface.h; y++)
                    {
                        for (int x = 0; x < source_surface.w; x++)
                            {
                                UInt32 pixel = GetPixel(source_surface_ptr, x, y);
                                
                                
                                (byte r, byte g, byte b, byte a) = GetPixelColorValues(source_surface_ptr, x, y);
                                Console.WriteLine($"Source Pixel ({x}, {y}): R: {r} G: {g} B: {b} A: {a}");

                                
                                int dest_x = source_surface.h - y - 1;
                                int dest_y = x;
                                SetPixel(dest_surface_ptr, dest_x, dest_y, pixel);
                                
                                
                                (byte dest_r, byte dest_g, byte dest_b, byte dest_a) = GetPixelColorValues(dest_surface_ptr, dest_x, dest_y);
                                Console.WriteLine($"Dest Pixel ({dest_x}, {dest_y}): R: {dest_r} G: {dest_g} B: {dest_b} A: {dest_a}");
                                SetPixel(dest_surface_ptr, dest_x, dest_y, pixel);

                                //
                                // // Check for transparency
                                // UInt32 alpha = pixel & 0xFF000000;
                                // if (alpha != 0)
                                //     {
                                //         SetPixel(dest_surface_ptr, dest_x, dest_y, pixel);
                                //     }
                            }
                    }
                
                SDL_UnlockSurface(source_surface_ptr);
                SDL_UnlockSurface(dest_surface_ptr);

                return dest_surface_ptr;
            }

        private static UInt32 GetPixel(IntPtr surface_ptr, int x, int y)
            {
                SDL_Surface surface =
                    Marshal.PtrToStructure<SDL_Surface>(surface_ptr);
                int bpp = 4; //RGBA8888
                IntPtr pixel = surface.pixels + y * surface.pitch + x * bpp;
                return (UInt32)Marshal.ReadInt32(pixel);
            }
        private static void SetPixel(IntPtr surface_ptr, int x, int y, UInt32 new_pixel)
            {
                SDL_Surface surface =
                    Marshal.PtrToStructure<SDL_Surface>(surface_ptr);
                int bpp = 4; //RGBA8888
                IntPtr pixel = surface.pixels + y * surface.pitch + x * bpp;
                
                byte originalAlpha = (byte)(Marshal.ReadInt32(pixel) & 0xFF);
    
                // If the original pixel's alpha value is 255, set the new pixel's alpha value to 255
                if (originalAlpha == 255)
                    {
                        new_pixel |= 0xFF000000;
                    }
                
                Marshal.WriteInt32(pixel, (Int32)new_pixel);
            }

    }
}