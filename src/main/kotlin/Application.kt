package aquadx

import io.ktor.http.*
import io.ktor.serialization.kotlinx.json.*
import io.ktor.server.application.*
import io.ktor.server.engine.*
import io.ktor.server.netty.*
import io.ktor.server.plugins.contentnegotiation.*
import io.ktor.server.plugins.statuspages.*
import io.ktor.server.response.*

fun main(args: Array<String>) {
    embeddedServer(Netty, port = 20100, module = Application::module).start()
    FutariRelay().start()
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
    configureRouting()
}
