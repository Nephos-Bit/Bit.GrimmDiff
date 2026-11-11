# GrimmDiff CRA product file

## Product and intended purpose

GrimmDiff is a Windows desktop Git client. It presents repository status, diffs and history; performs user-initiated Git operations; and optionally groups diff hunks through a signed-in ChatGPT Codex CLI session. The local Git functionality remains available without the optional AI feature.

The manufacturer, legal entity, postal address, digital contact, product version, support end date, and EU declaration of conformity must be entered in the commercial-release record before market placement. This file is the version-controlled source evidence for that record.

## Security architecture

Git commands are launched without a shell, external diff/text-conversion helpers are disabled, and commit hooks are disabled for application-initiated commits. The optional AI operation uses a local Codex CLI with a read-only sandbox, disabled web search, no approvals, timeout limits, structured output validation, and credential environment-variable removal.

AI processing is opt-in per repository. Approval is bound to the repository configuration fingerprint, provider identity, and `AiDiffSafetyPolicy` version. The application displays the transferable and excluded changed-section counts before approval and after analysis. Secrets, credentials, binary changes, oversized changes, and selected sensitive directories remain local.

## Required release evidence

- Threat model: `docs/security/threat-model.md`.
- Test, review, dependency-scan, SBOM, hash and signing results for the exact release.
- CycloneDX SBOM produced by `scripts/New-Sbom.ps1`.
- Signed executable and SHA-256 archive checksum produced by `scripts/Publish-Portable.ps1`.
- Completed CRA Annex I, II and VII checklist retained by the security owner.

## Release gate

Commercial release requires passing tests and dependency scanning, a reviewed SBOM, a valid code-signature verification, a published support end date, an active vulnerability contact, and manufacturer approval of the technical documentation and EU declaration of conformity.
