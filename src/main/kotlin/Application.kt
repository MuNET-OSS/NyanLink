package aquadx

import io.ktor.http.*
import io.ktor.serialization.kotlinx.json.*
import io.ktor.server.application.*
import io.ktor.server.engine.*
import io.ktor.server.netty.*
import io.ktor.server.plugins.contentnegotiation.*
import io.ktor.server.plugins.cors.routing.*
import io.ktor.server.plugins.statuspages.*
import io.ktor.server.response.*

fun main(args: Array<String>) {
    // Parse command line arguments with default port
    val lobbyPort = parsePort(args, "--lobby-port", "LOBBY_PORT", 20100)
    val relayPort = parsePort(args, "--relay-port", "RELAY_PORT", 20101)
    
    println("=== WorldLink Server Configuration ===")
    println("Lobby Port (HTTP API): $lobbyPort")
    println("Relay Port (Game Communication): $relayPort")
    println("=====================================")
    
    embeddedServer(Netty, port = lobbyPort, module = Application::module).start()
    FutariRelay(relayPort).start()
}

/**
 * Parse port from command line arguments or environment variables
 * Priority: Command line args > Environment variables > Default value
 */
private fun parsePort(args: Array<String>, argName: String, envName: String, defaultPort: Int): Int {
    // Check command line arguments first
    for (i in args.indices) {
        if (args[i] == argName && i + 1 < args.size) {
            return args[i + 1].toIntOrNull() ?: defaultPort
        }
    }
    
    // Fall back to environment variable
    return System.getenv(envName)?.toIntOrNull() ?: defaultPort
}

fun Application.module() {
    install(ContentNegotiation) {
        json(KJson)
    }
    install(StatusPages) {
        exception<ApiException> { call, cause ->
            call.respond(HttpStatusCode.fromValue(cause.code), cause.message ?: "Something went wrong")
        }
    }
    install(CORS) {
        anyHost()
    }
    configureRouting()
}
