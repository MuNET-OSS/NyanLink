package aquadx

import io.ktor.server.application.*
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.serialization.json.Json
import java.io.BufferedWriter
import java.io.File
import java.io.FileOutputStream
import java.time.LocalDateTime
import java.util.concurrent.ConcurrentHashMap

// KotlinX Serialization
val KJson = Json {
    ignoreUnknownKeys = true
    isLenient = true
    explicitNulls = false
    coerceInputValues = true
}

// Maximum time to live for a recruit record
const val MAX_TTL = 30 * 1000

val RecruitRecord.ip get() = RecruitInfo.MechaInfo.IpAddress

fun Application.configureRouting() = routing {
    // <IP Address, RecruitInfo>
    val recruits = ConcurrentHashMap<UInt, RecruitRecord>()
    // Append writer
    val writer: BufferedWriter = FileOutputStream(File("recruit.log"), true).bufferedWriter()
    val mutex = Mutex()
    val log = logger()

    suspend fun log(data: String) = mutex.withLock {
        log.info(data)
        writer.write(data)
        writer.newLine()
        writer.flush()
    }

    suspend fun log(data: RecruitRecord, msg: String) =
        log("${LocalDateTime.now().isoDateTime()}: $msg: ${KJson.encodeToString(data)}")

    get("/") {
        call.respondText("Running!")
    }

    get("/recruit/start") {
        val d = call.receive<RecruitRecord>().apply { Time = millis() }
        val exists = recruits.containsKey(d.ip)
        recruits[d.ip] = d

        if (!exists) log(d, "StartRecruit")
        d.RecruitInfo.MechaInfo.UserIDs = d.RecruitInfo.MechaInfo.UserIDs.map { it.str.hashToUInt().toLong() }
    }

    post("/recruit/finish") {
        val d = call.receive<RecruitRecord>()
        if (!recruits.containsKey(d.ip)) 404 - "Recruit not found"
        if (d.Keychip != recruits[d.ip]!!.Keychip) 400 - "Keychip mismatch"
        recruits.remove(d.ip)
        log(d, "EndRecruit")
    }

    get("/recruit/list") {
        val time = millis()
        recruits.filterValues { time - it.Time > MAX_TTL }.keys.forEach { recruits.remove(it) }
        call.respondText(recruits.values.toList().joinToString("\n") { KJson.encodeToString(it) })
    }

    get("/info") {
        call.respond(mapOf("relayHost" to "futari.aquadx.net", "relayPort" to 20101))
    }
}
