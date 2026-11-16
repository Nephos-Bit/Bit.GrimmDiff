# GrimmDiff CRA product file

## Product and intended purpose

GrimmDiff is a Windows desktop Git client. It presents repository status, diffs and history; performs user-initiated Git operations; and optionally groups diff hunks through a signed-in ChatGPT Codex CLI session. The local Git functionality remains available without the optional AI feature.

The manufacturer, legal entity, postal address, digital contact, product version, support end date, and EU declaration of conformity must be entered in the commercial-release record before market placement. This file is the version-controlled source evidence for that record.

## Legal effect and manufacturer obligations

These documents do not create independent legal duties. They evidence compliance with Regulation (EU) 2024/2847 (Cyber Resilience Act) only when GrimmDiff is made available on the EU market in the course of a commercial activity and the release owner is its manufacturer. Publishing non-monetised free and open-source software in a public GitHub repository does not by itself amount to making it available on the market; the CRA manufacturer duties below therefore do not apply to that distribution model. They apply if the manufacturer later monetises or otherwise commercially supplies GrimmDiff. This does not replace a case-specific assessment of scope, product classification, or conformity-assessment route.

The manufacturer must, for each commercial release:

- design, develop, deliver, and maintain the product according to the Annex I cybersecurity requirements; keep a documented and current cybersecurity risk assessment; exercise due diligence for third-party and open-source components; and remediate their vulnerabilities;
- define and publish a support period. It is normally at least five years unless the expected product use is shorter; provide security updates during that period and retain each issued update for at least ten years or the remaining support period, whichever is longer;
- operate a coordinated vulnerability-disclosure process with a clearly identifiable contact point, notify affected users of material vulnerabilities or severe incidents and provide mitigations and fixes;
- retain technical documentation (including the risk assessment, SBOM, vulnerability-handling process, test evidence, support-period rationale, and user instructions), complete the applicable conformity assessment, issue the EU declaration of conformity, apply the CE marking, and retain the technical documentation and declaration for at least ten years after market placement or for the support period, whichever is longer;
- cooperate with market-surveillance authorities and take corrective action, including withdrawal or recall where required, if the product is non-compliant.

Article 14 reporting duties already apply from 11 September 2026, including to products made available before the general CRA application date. An actively exploited vulnerability or a severe security incident requires an early warning through the CRA Single Reporting Platform within 24 hours of awareness, a notification within 72 hours, and a final report within 14 days after a corrective measure is available (actively exploited vulnerability) or within one month after the 72-hour notification (severe incident). The remaining CRA obligations apply from 11 December 2027.

Primary sources: [Regulation (EU) 2024/2847, Articles 13, 14 and 71, and Annexes I, II and VII](https://eur-lex.europa.eu/eli/reg/2024/2847/oj); [European Commission CRA implementation summary](https://digital-strategy.ec.europa.eu/en/policies/cra-summary).

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
