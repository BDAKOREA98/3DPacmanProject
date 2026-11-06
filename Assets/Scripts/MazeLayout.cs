/// <summary>
/// 미로 배치도 데이터. 28열 x 31행이며 위쪽 행이 +Z 방향이다.
/// '#' = 벽, 'o' = 일반 아이템, 'P' = 큰 아이템(파워 펠릿), '.' = 아이템 없는 길/빈 공간.
/// </summary>
public static class MazeLayout
{
    public const char Wall = '#';
    public const char Item = 'o';
    public const char BigItem = 'P';

    /// <summary>양 끝이 뚫려 있어 순간이동이 일어나는 터널 행 번호(0부터, 위에서 아래로).</summary>
    public const int TunnelRow = 14;

    public static readonly string[] Rows =
    {
        "############################",
        "#oooooooooooo##oooooooooooo#",
        "#o####o#####o##o#####o####o#",
        "#P####o#####o##o#####o####P#",
        "#o####o#####o##o#####o####o#",
        "#oooooooooooooooooooooooooo#",
        "#o####o##o########o##o####o#",
        "#o####o##o########o##o####o#",
        "#oooooo##oooo##oooo##oooooo#",
        "######o#####.##.#####o######",
        ".....#o#####.##.#####o#.....",
        ".....#o##..........##o#.....",
        ".....#o##.###..###.##o#.....",
        "######o##.#......#.##o######",
        "......o...#......#...o......",
        "######o##.#......#.##o######",
        "######o##.########.##o######",
        ".....#o##..........##o#.....",
        ".....#o##.########.##o#.....",
        "######o##.########.##o######",
        "#oooooooooooo##oooooooooooo#",
        "#o####o#####o##o#####o####o#",
        "#P####o#####o##o#####o####P#",
        "#ooo##oooooooooooooooo##ooo#",
        "###o##o##o########o##o##o###",
        "###o##o##o########o##o##o###",
        "#oooooo##oooo##oooo##oooooo#",
        "#o##########o##o##########o#",
        "#o##########o##o##########o#",
        "#oooooooooooooooooooooooooo#",
        "############################",
    };

    public static int RowCount => Rows.Length;
    public static int ColCount => Rows[0].Length;
}
