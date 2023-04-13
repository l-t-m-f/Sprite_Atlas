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
        internal static IntPtr renderer;

        static void Main(string[] args)
            {
                setup();
                Atlas atlas = new();
                const string search_pattern = "*.png";
                var file_names =
                    Directory.GetFiles(DIRECTORY_PATH, search_pattern);

                foreach (var file_name in file_names)
                    {
                        var file_path = Path.GetRelativePath(".", file_name);
                        //Console.WriteLine(filePath);
                        atlas.surface_data[atlas.entries.Count] = IMG_Load(file_path);
                        atlas.entries.Add(new AtlasEntry(file_path, new SDL_Rect { x = 0, y = 0, w = 0, h = 0 }, false));

                    }

                Array.Resize(ref atlas.surface_data, atlas.entries.Count);
                Array.Sort(atlas.surface_data, atlas);

                for (var i = 0; i < atlas.surface_data.Length; i++)
                    {
                        SDL_QueryTexture(
                            SDL_CreateTextureFromSurface(renderer,
                                atlas.surface_data[i]), out _, out _, out int w,
                            out int h);

                        var rotated = false;
                        var found_node = Atlas.find_node(atlas.first, w, h);

                        if (found_node == null)
                            {
                                rotated = true;
                                found_node = Atlas.find_node(atlas.first, h, w);
                            }

                        if (found_node != null)
                            {
                                Console.WriteLine($"Node found for image #{i}");

                                //int rotations = 0;
                                
                                
                                if(rotated)
                                    {
                                        found_node.height = w;
                                        found_node.width = h;
                                        //rotations++;
                                    }
                                
                                var dest = new SDL_Rect
                                    {
                                        x = found_node.x,
                                        y = found_node.y,
                                        w = found_node.width,
                                        h = found_node.height
                                    };

                                atlas.entries[i] = new AtlasEntry(atlas.entries[i].filename, dest, rotated);

                                if (rotated == false)
                                    {
                                        SDL_BlitSurface(atlas.surface_data[i],
                                            IntPtr.Zero,
                                            atlas.master_surface,
                                            ref dest);
                                    }
                                else
                                    {
                                        IntPtr result = blit_rotated(atlas.surface_data[i]);
                                        if (result != IntPtr.Zero)
                                            {
                                                
                                                SDL_BlitSurface(result,
                                                IntPtr.Zero,
                                                atlas.master_surface,
                                                    ref dest);
                                            }
                                    }
                            }
                        SDL_FreeSurface(atlas.surface_data[i]);
                    }

                var master_texture =
                    SDL_CreateTextureFromSurface(renderer, atlas.master_surface);
                var test_extract =
                    atlas.get_atlas_image("Images\\Tear.png");
                SDL_QueryTexture(test_extract, out _, out _,
                    out var extract_w,
                    out var extract_h);

                var dst_rect = new SDL_Rect
                        { x = 50, y = 50, w = extract_w, h = extract_h };
                var dst_rect2 = new SDL_Rect
                    {
                        x = 0, y = 0, w = Atlas.ATLAS_SIZE, h = Atlas.ATLAS_SIZE
                    };

                foreach (var ae in atlas.entries)
                    {
                        Console.WriteLine(ae.filename);
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

                        SDL_SetRenderDrawColor(renderer, 255, 0, 0, 255);
                        SDL_RenderClear(renderer);
                        SDL_RenderCopy(renderer,
                            master_texture,
                            0, ref dst_rect2);
                        SDL_RenderPresent(renderer);
                    }
            }

        private static void setup()
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

                renderer = SDL_CreateRenderer(_window, -1,
                    SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                    SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

                if (renderer == IntPtr.Zero)
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
        
        private static (byte r, byte g, byte b, byte a) get_pixel_color_values(IntPtr surface_ptr, int x, int y)
            {
                uint pixel = get_pixel(surface_ptr, x, y);
                byte r = (byte)((pixel >> 24) & 0xFF);
                byte g = (byte)((pixel >> 16) & 0xFF);
                byte b = (byte)((pixel >> 8) & 0xFF);
                byte a = (byte)(pixel & 0xFF);
                return (r, g, b, a);
            }


        private static IntPtr blit_rotated(IntPtr source_surface_ptr)
            {
                var source_surface =
                    Marshal.PtrToStructure<SDL_Surface>(source_surface_ptr);
                
                Console.WriteLine($"Source surface dimensions: {source_surface.w}x{source_surface.h}");

                
                var dest_surface_ptr = SDL_CreateRGBSurfaceWithFormat(0,
                    source_surface.h, source_surface.w, 32,
                    SDL_PIXELFORMAT_ARGB8888);

                SDL_LockSurface(source_surface_ptr);
                SDL_LockSurface(dest_surface_ptr);

                for (var y = 0; y < source_surface.h; y++)
                    {
                        for (var x = 0; x < source_surface.w; x++)
                            {
                                var pixel = get_pixel(source_surface_ptr, x, y);
                                var (r, g, b, a) = get_pixel_color_values(source_surface_ptr, x, y);
                                Console.WriteLine($"Source Pixel ({x}, {y}): R: {r} G: {g} B: {b} A: {a}");
                                var dest_x = source_surface.h - y - 1;
                                set_pixel(dest_surface_ptr, dest_x, x, pixel);

                                var (dest_r, dest_g, dest_b, dest_a) = get_pixel_color_values(dest_surface_ptr, dest_x, x);
                                Console.WriteLine($"Dest Pixel ({dest_x}, {x}): R: {dest_r} G: {dest_g} B: {dest_b} A: {dest_a}");
                                set_pixel(dest_surface_ptr, dest_x, x, pixel);

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

        private static uint get_pixel(IntPtr surface_ptr, int x, int y)
            {
                var surface =
                    Marshal.PtrToStructure<SDL_Surface>(surface_ptr);
                const int bytes_per_pixel = 4; //RGBA8888
                var pixel = surface.pixels + y * surface.pitch + x * bytes_per_pixel;
                return (uint)Marshal.ReadInt32(pixel);
            }
        private static void set_pixel(IntPtr surface_ptr, int x, int y, uint new_pixel)
            {
                var surface =
                    Marshal.PtrToStructure<SDL_Surface>(surface_ptr);
                const int bytes_per_pixel = 4; //RGBA8888
                var pixel = surface.pixels + y * surface.pitch + x * bytes_per_pixel;
                
                
                Marshal.WriteInt32(pixel, (int)new_pixel);
            }

    }
}