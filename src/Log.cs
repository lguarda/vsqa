using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;

namespace TestHarnessMod {
    public static class Logger {
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;

        public static void slog(string msg) {
            sapi.Logger.Notification(msg);
        }

        public static void clog(string msg, bool chat = false) {
            if (chat) capi.ShowChatMessage(msg);
            capi.Logger.Notification(msg);
        }
    }
}

