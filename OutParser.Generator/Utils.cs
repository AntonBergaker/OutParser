namespace OutParser.Generator;
public static class Utils {

    public static char GetCharSafe(string str, int index) {
        if (index < 0 || index >= str.Length) {
            return '\0';
        }
        return str[index];
    }
}
