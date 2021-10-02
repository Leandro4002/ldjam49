using System;

namespace ldjam49Namespace {
    public static class Program {
        [STAThread]
        static void Main() {
            using (var game = new Ldjam49())
                game.Run();
        }
    }
}
