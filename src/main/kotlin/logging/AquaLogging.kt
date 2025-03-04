package aquadx.logging

import org.tinylog.writers.AbstractFormatPatternWriter

object MinecraftColor {

    // 1. Create a list of pairs (code -> escape)
    //    We convert Python’s r[:2] -> code, r[3:] -> escape
    private val MINECRAFT_COLORS = listOf(
        "&0" to "\u001B[38;5;0m",
        "&1" to "\u001B[38;5;4m",
        "&2" to "\u001B[38;5;2m",
        "&3" to "\u001B[38;5;6m",
        "&4" to "\u001B[38;5;1m",
        "&5" to "\u001B[38;5;5m",
        "&6" to "\u001B[38;5;3m",
        "&7" to "\u001B[38;5;7m",
        "&8" to "\u001B[38;5;8m",
        "&9" to "\u001B[38;5;12m",
        "&a" to "\u001B[38;5;10m",
        "&b" to "\u001B[38;5;14m",
        "&c" to "\u001B[38;5;9m",
        "&d" to "\u001B[38;5;13m",
        "&e" to "\u001B[38;5;11m",
        "&f" to "\u001B[38;5;15m",

        // Formatting
        "&l" to "\u001B[1m",   // Bold
        "&o" to "\u001B[3m",   // Italic
        "&n" to "\u001B[4m",   // Underline
        "&k" to "\u001B[8m",   // Hidden
        "&m" to "\u001B[9m",   // Strikethrough
        "&r" to "\u001B[0m",   // Reset

        // Extended codes
        "&-" to "\n",          // Line break
        "&~" to "\u001B[39m",  // Reset text color
        "&*" to "\u001B[49m",  // Reset background color
        "&L" to "\u001B[22m",  // Disable bold
        "&O" to "\u001B[23m",  // Disable italic
        "&N" to "\u001B[24m",  // Disable underline
        "&K" to "\u001B[28m",  // Disable hidden
        "&M" to "\u001B[29m"   // Disable strikethrough
    )

    // 2. Data class to build the ANSI escape string for RGB values
    private data class RGB(val r: Int, val g: Int, val b: Int) {
        fun toAnsi(foreground: Boolean): String {
            // 38 -> foreground, 48 -> background
            return "\u001B[" + (if (foreground) "38" else "48") + ";2;${r};${g};${b}m"
        }
    }

    // 3. Equivalent of the Python color(msg) function
    fun color(input: String): String {
        var msg = input

        // Replace all simple color codes first
        for ((code, esc) in MINECRAFT_COLORS) {
            msg = msg.replace(code, esc)
        }

        // Handle the extended &gf(...) / &gb(...) custom RGB logic
        // Foreground if 'f' or background if 'b'
        while (msg.contains("&gf(") || msg.contains("&gb(")) {
            val gfIndex = msg.indexOf("&gf(")
            val gbIndex = msg.indexOf("&gb(")

            // Find whichever appears first (if they exist)
            val i = when {
                gfIndex == -1 && gbIndex == -1 -> break
                gfIndex == -1 -> gbIndex
                gbIndex == -1 -> gfIndex
                else -> minOf(gfIndex, gbIndex)
            }
            // i now points to "&gf(" or "&gb("

            val end = msg.indexOf(')', i)
            if (end == -1) break // No closing parenthesis -> stop

            val code = msg.substring(i + 4, end)
            val fore = msg.substring(i + 2, i + 3) == "f"  // 'f' vs 'b'

            val rgb = if (code.startsWith("#")) {
                // Parse hex: #RRGGBB
                val hex = code.removePrefix("#")
                Triple(
                    hex.substring(0, 2).toInt(16),
                    hex.substring(2, 4).toInt(16),
                    hex.substring(4, 6).toInt(16)
                )
            } else {
                // Parse "r g b" or "r,g,b" or "r;g;b"
                val parts = code.replace(",", " ")
                    .replace(";", " ")
                    .split(" ")
                    .filter { it.isNotBlank() }
                    .map { it.toInt() }
                Triple(parts[0], parts[1], parts[2])
            }

            val ansi = RGB(rgb.first, rgb.second, rgb.third).toAnsi(fore)
            msg = msg.substring(0, i) + ansi + msg.substring(end + 1)
        }

        return msg
    }

    // 4. Equivalent of Python’s printc(msg)
    fun printc(msg: String) {
        println(color("$msg&r")) // Force reset at the end
    }
}

class AquaWriter(properties: Map<String?, String?>?) : AbstractFormatPatternWriter(properties) {
    override fun write(logEntry: org.tinylog.core.LogEntry) {
        print(MinecraftColor.color(render(logEntry)
            .replaceFirst("DEBUG", "&8DEBUG")
            .replaceFirst("TRACE", "&8TRACE")
            .replaceFirst("INFO", "&aINFO")
            .replaceFirst("WARN", "&eWARN")
            .replaceFirst("ERROR", "&cERROR") + "&r"
        ))
    }

    override fun flush() {
        System.out.flush()
    }

    override fun close() {
        // System.out doesn't have to be closed
    }
}