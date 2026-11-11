# Threat model

| Asset / boundary | Primary threat | Control | Verification |
|---|---|---|---|
| Opened Git repository | Malicious Git config, external diff helpers, hooks | Shell-free argument lists; external helpers disabled; `core.hooksPath` empty for commits | Negative Git-command tests and manual hostile-repository test |
| Remote Git operation | Unsafe protocol handler | `protocol.ext.allow=never`; user-initiated network operation | Command-construction test |
| AI data boundary | Secrets, binary data, oversized diff, prompt injection | Repository approval; safety policy; read-only CLI; disabled web search; structured output | Filter tests and provider prompt inspection |
| Codex CLI process | Credential exposure or filesystem modification | Credential environment removal, ephemeral read-only process, timeout and cancellation | Process-request test |
| Release artifact | Tampering or unknown components | Code signing, SHA-256 checksum, CycloneDX SBOM, CI release gate | Signature and hash validation |

The security owner reviews this model before a material architecture change and at least once per supported major release.
