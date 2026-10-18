using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public static class Square
{
    public static string Name(int sq) => $"{(char)('a' + sq % 8)}{sq / 8 + 1}";
    public static bool TryParse(string text, out int square)
    {
        square = -1;
        if (text.Length != 2 || text[0] < 'a' || text[0] > 'h' || text[1] < '1' || text[1] > '8')
            return false;
        square = text[0] - 'a' + 8 * (text[1] - '1');
        return true;
    }

    public static bool OnBoard(int file, int rank) => file is >= 0 and < 8 && rank is >= 0 and < 8;
    public static int At(int file, int rank) => rank * 8 + file;
}
