# Project Lotus: Security Policy

---

The community is our top priority, and we take vulnerability reports incredibly seriously.
Please do not make false security reports, doing so may have consequences.

Bugs are not security vulnerabilities, however security vulnerabilities are bugs. Please do not report bugs as security vulnerabilities.
If you need to report non-serious bugs, please use our bug-reports forum in our Discord.
---
## 1. Reporting a Vulnerability
If you believe you have found a security vulnerability in Lotus, report it privately through our **[Discord](https://discord.gg/projectlotus) by opening a ticket.** <br />
Alternatively, you can **email <a href="mailto:project@lotusau.top?subject=Reporting a Security Vulnurability">project@lotusau.top</a>**

We aim to respond to all security reports within 24 hours, however we may take longer if we need to investigate further.
### What to include
Please include the following:
- The version of Lotus you are using.
- Your environment (OS & Platform which you play Among Us on.)
- A description of the vulnerability.
- Steps to reproduce the vulnerability.
- Client Logs (from `\BepInEx\LogOutput`, and `\logs`)
- (Optional, but recommended) A video or screenshot of the vulnerability.
- (Optional) Code Reference.

While you may not be able to provide all information, all which you CAN provide will be incredibly helpful.

---
## 2. Expected Response Time & Disclosure
We aim to respond to all security reports within 24 hours, however we may take longer if we need to investigate further.

Please **do not publically disclose security vulnerabilities** until a release has been created which fixes the vulnerability.

## Scope
This policy applies to:
- Vulnerabilities in the mod's code which could affect users of the mod or members of the community.
  - Example: An exploit which could result in a malicious actor gaining access to a user's computer or data outside of Among Us.
- In-game bugs or exploits which could cause serious hinderance to the gameplay of players. 
  - Example: An exploit which causes the player's game to crash.

---
## Example Report:
```terminaloutput
Mod Version: `v1.8.0`
Envrionment: `Windows 11, Steam`
Description: `This vulnerability allows for a malicious player to crash a player's game and computer by sending a large amount of data to the host.`
Steps to Reproduce:
    - `Open two instances of the game, one will act as the host, while the second will be the malicious actor.`
    - `Download (example addon) on the malicious instance, then join the host's game`
    - `Press the 'Crash Host" button, you will see the host's instance freeze and eventually close.`
[Attached Client Logs from host's instance]
[A video of the exploit occurring]
[A link to the example addon which abuses the exploit]
```

---
We appreciate you for helping us keep Lotus safe for all users! --- lotus forever 🪷