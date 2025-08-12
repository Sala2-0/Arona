<img width="680" height="240" alt="wows_ba_banner" src="https://github.com/user-attachments/assets/bfe94411-e04e-444a-b062-c40558867c14" />

## Arona - A clan battle oriented discord bot for World of Warships
Add it to your server with this [link](https://discord.com/oauth2/authorize?client_id=1360295816476098692&permissions=139586750464&integration_type=0&scope=bot+applications.commands)

It is recommended that you create a special channel where Arona can log events in.

Bot theme is based on [Blue Archive](https://www.nexon.com/main/en/Blue%20Archive/details)

## Commands and features
### Success Factor
Success Factor (S/F) is an experimental statistics ratio aiming to show a clans overall success over a CB season.

The hope is that S/F could give a new perspective on a clans position on the leaderboards.
<br> For example, S/F could determine whether a clan spammed games or put in actual effort to reach their position on the leaderboards.

Formula: 
  
$\Large \frac{\frac{rating^{L}}{15} \times \frac{rating}{battles}}{10}$

where L is based on what league the clan currently resides in.

| League  | Exponent (L) value |
|---------|--------------------|
|Hurricane|1                   |
|Typhoon  |0.8                 |
|Storm    |0.6                 |
|Gale     |0.4                 |
|Squall   |0.2                 |

Since exponent is different per league, each league would have their own ranges on what's considered "good" or "bad" S/F.

### Clan Monitor

Monitors a clans CB activity and displays it to you.
| Command Name         | Description                                                              |
|----------------------|--------------------------------------------------------------------------|
|`/clan_monitor_add`   |Add a clan to server database                                             |
|`/clan_monitor_remove`|Remove a clan from server database                                        |
|`/clan_monitor_list`  |List all clans currently monitored in the server where the command is used|

### Builds
Store your [WoWs ShipBuilder](https://app.wowssb.com) builds/links for ease of access later on.
| Command Name         | Description                                                              |
|----------------------|--------------------------------------------------------------------------|
|`/builds_add`         |Add a build to server database                                            |
|`/builds_remove`      |Remove a build from server database                                       |
|`/builds_get`         |Get a build saved in server database                                      |

### Other commands
| Command Name         | Description                                                                                                                                        |
|----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|
|`/pr_calculator`      |Calculates the personal rating of any publically available ship <br> Values and formula from [wows-numbers](https://wows-numbers.com)               |
|`/prime_time`         |Get a clans current CB session selection and activity                                                                                               |
|`/leaderboard`        |Latest CB seasons leaderboards, globally and regionally                                                                                             |
|`/ratings`            |Get detailed info about a clans CB rating on latest season, their progress on league qualifications <br> as well as their global and region rankings|
|`/set_channel`        |Set a text channel for Arona to log events in                                                                                                       |

## License
Licensed under MIT.

Any redistributions, use or derivative work of any kind is required to have a link to original repository
(https://github.com/Sala2-0/Arona) in README or equivalent documentation
