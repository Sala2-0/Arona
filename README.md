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

<h3>Clan Monitor</h3>
<p>Monitors a clans CB activity and displays it to you.</p>
<table border="0.5" cellspacing="0" cellpadding="5">
  <tr>
    <th>Command Name</th>
    <th>Description</th>
  </tr>
  <tr>
    <td>/clan_monitor add</td>
    <td>Add a clan to server database</td>
  </tr>
  <tr>
    <td>/clan_monitor remove</td>
    <td>Remove a clan from server database</td>
  </tr>
  <tr>
    <td>/clan_monitor list</td>
    <td>List all clans currently monitored in the server where the command is used</td>
  </tr>
</table>

<h3>Builds</h3>
<p>Store your <a href="https://app.wowssb.com" target="_blank">WoWs ShipBuilder</a> builds/links for ease of access later on.</p>
<table border="0.5" cellspacing="0" cellpadding="5">
  <tr>
    <th>Command Name</th>
    <th>Description</th>
  </tr>
  <tr>
    <td>/builds add</td>
    <td>Add a build to server database</td>
  </tr>
  <tr>
    <td>/builds remove</td>
    <td>Remove a build from server database</td>
  </tr>
  <tr>
    <td>/builds get</td>
    <td>Get a build saved in server database</td>
  </tr>
</table>

<h3>Other commands</h3>
<table border="0.5" cellspacing="0" cellpadding="5">
  <tr>
    <th>Command Name</th>
    <th>Description</th>
  </tr>
  <tr>
    <td>/pr_calculator single</td>
    <td>
      Calculates PR of a ship for one battle. Values and formula from <a href="https://wows-numbers.com" target="_blank">wows-numbers</a>
    </td>
  </tr>
  <tr>
    <td>/pr_calculator session</td>
    <td>
      Calculates PR of one session. Each battle should have 4 parameters (name, damage, kills, outcome (win, loss)) separated by commas and should be separated by underscores
      <table border="0.5" cellspacing="0" cellpadding="3" style="margin-top:5px;">
        <tr>
          <th>Parameter</th>
          <th>Description</th>
        </tr>
        <tr>
          <td>name</td>
          <td>Name of the ship. You don't need to type the full name, but enough for the bot to figure it out.</td>
        </tr>
        <tr>
          <td>damage</td>
          <td>Damage dealt. Must be whole number</td>
        </tr>
        <tr>
          <td>kills</td>
          <td>Number of kills. Must be whole number</td>
        </tr>
        <tr>
          <td>Outcome</td>
          <td>Win or loss. Must be 'win' or 'loss'</td>
        </tr>
      </table>
      Example input:
      <pre><code>Småland,120000,3,win_Kurfurst,220000,1,loss</code></pre>
    </td>
  </tr>
  <tr>
    <td>/prime_time</td>
    <td>Get a clan's current CB session selection and activity</td>
  </tr>
  <tr>
    <td>/leaderboard</td>
    <td>Latest CB seasons leaderboards, globally and regionally</td>
  </tr>
  <tr>
    <td>/ratings</td>
    <td>
      Get detailed info about a clan's CB rating on latest season, their progress on league qualifications<br>
      as well as their global and region rankings
    </td>
  </tr>
  <tr>
    <td>/set_channel</td>
    <td>Set a text channel for Arona to log events in</td>
  </tr>
</table>

## License
Licensed under MIT.

Any redistributions, use or derivative work of any kind is required to have a link to original repository
(https://github.com/Sala2-0/Arona) in README or equivalent documentation
