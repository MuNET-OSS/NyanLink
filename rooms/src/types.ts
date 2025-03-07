export interface MusicInfo {
    name: string,
    composer: string,
    notes: {
        lv: number,
    }[]
}

export interface Server {
    code: string
    name: string
    url: string
    data: RecruitInfo[]
}

export interface RecruitInfo {
    MechaInfo: {
        IsJoin: boolean
        IpAddress: number
        MusicID: number
        Entrys: boolean[]
        UserIDs: number[]
        UserNames: string[]
        IconIDs: number[]
        FumenDifs: number[]
        Rateing: number[]
        ClassValue: number[]
        MaxClassValue: number[]
        UserType: number[]
    }
    MusicID: number
    GroupID: number
    EventModeID: boolean
    JoinNumber: number
    PartyStance: number
    _startTimeTicks: number
    _recvTimeTicks: number

    music: MusicInfo
    users: RecruitUser[]
}

export interface RecruitUser {
    name: string
    rating: number
}