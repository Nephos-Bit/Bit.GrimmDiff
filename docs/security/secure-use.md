# Secure use and lifecycle information

Install GrimmDiff only from the signed release archive and verify its published SHA-256 checksum. Keep Windows, Git, and GrimmDiff on supported versions. Do not open untrusted repositories unless their Git content and remotes are acceptable for your environment.

Local Git functionality does not require the AI feature. AI analysis is optional and requires approval for each repository. Before approval, GrimmDiff shows how many changed sections and characters may be transferred to the signed-in ChatGPT workspace and how many sections remain local. Revoke an approval from the application menu at any time.

To decommission, revoke repository AI approvals, sign out of the Codex CLI, and remove `%LOCALAPPDATA%\GrimmDiff\settings.json`. This removes GrimmDiff preferences and stored repository approval fingerprints; it does not delete repositories or Git credentials managed by other software.
