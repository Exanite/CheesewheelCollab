using System;
using SDL2;

namespace Source.Sdl
{
    public static class SdlContext
    {
        private static int refCount = 0;

        public static void Start()
        {
            if (refCount == 0)
            {
                if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO) != 0)
                {
                    throw new Exception(SDL.SDL_GetError());
                }
            }

            refCount++;
        }

        public static void Stop()
        {
            if (refCount == 0)
            {
                throw new Exception($"{typeof(SdlContext).Name} has not been started.");
            }

            refCount--;

            if (refCount == 0)
            {
                SDL.SDL_Quit();
            }
        }
    }
}
