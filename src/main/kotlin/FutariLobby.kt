package aquadx

import com.alibaba.fastjson2.parseObject
import com.alibaba.fastjson2.toJSONString
import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.http.content.*
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

// <IP Address, RecruitInfo>
val recruits = ConcurrentHashMap<UInt, RecruitRecord>()
// Append writer
val writer: BufferedWriter = FileOutputStream(File("recruit.log"), true).bufferedWriter()
val mutex = Mutex()

fun Application.configureRouting() = routing {
    val log = logger()
    val hostOverride = System.getenv("HOST_OVERRIDE")

    suspend fun log(data: String) = mutex.withLock {
        log.info(data)
        writer.write(data)
        writer.newLine()
        writer.flush()
    }

    suspend fun log(data: RecruitRecord, msg: String) =
        log("${LocalDateTime.now().isoDateTime()}: $msg: ${KJson.encodeToString(data)}")

    staticResources("/", "dist")

    post("/recruit/start") {
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
        call.respondText(recruits.values.toList().joinToString("\n") { obj ->
            KJson.encodeToString(obj).parseObject().apply {
                this.remove("Keychip")
                this.remove("Time")
            }.toJSONString()
        })
    }

    get("/info") {
        mapOf(
            "relayHost" to (hostOverride ?: call.request.local.serverHost),
            "relayPort" to 20101
        ).ok()
    }

    get("/online") {
        val time = millis()
        // Clean up expired recruits
        recruits.filterValues { time - it.Time > MAX_TTL }.keys.forEach { recruits.remove(it) }
        
        // Count actual connected clients from FutariRelay
        val totalUsers = clients.size
        
        // Count active recruits (unique users)
        val activeRecruits = recruits.size
        
        OnlineUserInfo(
            totalUsers = totalUsers,
            activeRecruits = activeRecruits
        ).ok()
    }

    get("/debug") {
        mapOf(
            "serverHost" to call.request.local.serverHost,
            "remoteHost" to call.request.local.remoteHost,
            "localHost" to call.request.local.localHost,
            "serverPort" to call.request.local.serverPort,
            "remotePort" to call.request.local.remotePort,
            "localPort" to call.request.local.localPort,
            "uri" to call.request.uri,
            "method" to call.request.httpMethod.value,
            "headers" to call.request.headers.entries().joinToString("\n") { (k, v) -> "$k: $v" }
        ).ok()
    }
}

inline fun <reified T> T.ok() { throw ApiException(200, toJSONString()) }
