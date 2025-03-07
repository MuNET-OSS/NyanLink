<script lang="ts">
  import { onMount } from 'svelte'
  import type { MusicInfo, Server } from './types'

  const srvs = [
    'use', 'usw', 'asia', 'euro'
  ]
  const names = [
    "US East", "US West", "Asia", "Europe", "China"
  ]
  const urls = srvs.map(v => `${v}.link.aquadx.net`)

  let lst: Server[] = []

  // allMusic: map< str musicid : music info >
  let allMusic: Record<string, MusicInfo> = {}

  async function getData() {
    fetch("https://aquadx.net/d/mai2/00/all-music.json").then(res => res.json()).then(data => {
      allMusic = data
      console.log(allMusic)
    })
    const res = await Promise.all(urls.map(url => fetch(`https://corsproxy.io/?url=https://${url}/recruit/list`)))
    const data = await Promise.all(res.map(r => r.text()))
    const json = data.map(d => 
      d.split('\n').filter(v => v.length > 0).map(v => JSON.parse(v).RecruitInfo)
    )
    
    json.forEach((v, i) => {
      v.forEach(info => {
        info.music = allMusic[info.MusicID.toString()]
        info.users = new Array(info.MechaInfo.Entrys[1] ? 2 : 1).fill(0).map((_, i) => ({
          name: info.MechaInfo.UserNames[i],
          rating: info.MechaInfo.Rateing[i]
        }))
      })
    })

    lst = srvs.map((v, i) => ({
      code: v,
      name: names[i],
      url: urls[i],
      data: json[i]
    }))
    console.log(lst)
  }
  onMount(getData)
  
  // Refresh every 5s
  setInterval(getData, 5000)

  function imgError(e: any) {
    e.target.src = "https://aquadx.net/assets/imgs/no_cover.jpg"
  }
</script>

<main>
  <h1>WorldLink Server Rooms</h1>

  {#each lst as srv}
    <div class="server">
      <div class="srv-info">
        <h2>{srv.name}</h2>
        <span>{srv.url} - {srv.data.length} rooms</span>
      </div>

      <div class="rooms">
        {#each srv.data as info}
          <div class="room level-{info.MechaInfo.FumenDifs[0]}">
            <img src="https://aquadx.net/d/mai2/music/00{info.MusicID % 10000}.png" on:error={imgError}>

            <div class="info">
              <div class="song">
                <div class="name">{info.music?.name ?? "(unknown)"}</div>
                <div class="composer">{info.music?.composer ?? "(unknown)"}</div>
              </div>
              <div class="difficulty">
                {info.music?.notes[info.MechaInfo.FumenDifs[0]].lv.toFixed(1) ?? 0}</div>
            </div>

            <div class="users">
              {#each info.users as user}
                <div class="user">
                  <div class="name">{user.name}</div>
                  <div class="rating">{user.rating}</div>
                </div>
              {/each}
            </div>
          </div>
        {/each}
        {#if srv.data.length === 0}
          <div class="no-rooms">No rooms available</div>
        {/if}
      </div>
    </div>
  {/each}
</main>

<style lang="sass">
  $darker: rgba(0,0,0,0.2)

  .srv-info
    h2
      margin: 0
    
    span
      font-size: 0.8em
      opacity: 0.8

  .server
    margin: 20px
    padding: 20px 10px
    padding-bottom: 30px
    border-radius: 20px
    background: $darker

    display: flex
    flex-direction: column
    gap: 10px

  .rooms
    display: flex
    flex-wrap: wrap
    gap: 20px
    justify-content: center

  .no-rooms
    opacity: 0.5

  .room
    border-radius: 20px
    width: 200px
    height: 200px
    position: relative
    overflow: hidden

    border: 1px solid rgb(var(--lv-color))
    // Drop shadow glow lvcolor
    box-shadow: 0 0 10px 5px rgba(var(--lv-color), 0.1)

    .info, .users
      position: absolute
      width: 100%
      padding: 0 15px
      backdrop-filter: blur(5px)
      background: rgba(0,0,0,0.3)
      height: 50px
      box-sizing: border-box

      display: flex
      flex-direction: column
      justify-content: center

    img
      position: absolute
      inset: 0
      width: 100%
      height: 100%
    
    div
      z-index: 30

    .info
      flex-direction: row
      justify-content: space-between
      align-items: center
      gap: 5px

      .song
        text-align: left
        min-width: 0

        *
          white-space: nowrap
          text-overflow: ellipsis
          overflow: hidden

        .name
          margin-bottom: -3px
          font-weight: bold

        .composer
          opacity: 0.6
          font-size: 0.7em

      .difficulty
        font-size: 1em
        font-weight: bold
        top: 10px
        right: 15px
        
        border-radius: 0 0 0 20px

        display: flex
        justify-content: center
        align-items: center

    .users
      bottom: 0
      justify-content: center

      .user
        display: flex
        justify-content: space-between
        font-size: 0.8em

</style>
