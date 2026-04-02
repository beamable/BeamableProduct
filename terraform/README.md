# Terraform

This folder contains Terraform manifests used by BeamableProduct CI and maintainers to manage simple infrastructure (examples: S3 modules and environment configurations). The repo includes reusable modules and one or more environment folders that the CI workflow operates against.

## Key locations
- Modules: `terraform/modules/` (example: `terraform/modules/s3`)
- Environments: `terraform/environments/<name>/` (example: `terraform/environments/main/main.tf`)
- CI workflow: `.github/workflows/runTerraform.yml` (the GitHub Action that runs `terraform init/plan/apply` for the selected environment)

## Prerequisites
- Terraform: the repository CI uses Terraform `1.3.7` (see `.github/workflows/runTerraform.yml`). Use your preferred installer (`tfenv`, package manager) to match the workflow when running locally.
- AWS credentials or other cloud provider credentials configured in your shell environment or via a credentials file as required by your provider backend.
- Do not store secrets in Git. Use environment variables, secret manager, or CI secrets for sensitive values.

## Local usage
1. Choose an environment folder, for example `terraform/environments/main`.
2. Change into the environment directory:
   - `cd terraform/environments/main`
3. Initialize Terraform (optionally provide backend config):
   - `terraform init`
4. Validate and plan:
   - `terraform validate`
   - `terraform plan -out=tfplan`
5. Apply (use caution — this will change remote infrastructure):
   - `terraform apply ./tfplan`

## Notes and best practices
- The CI workflow runs Terraform in a non-interactive environment. When running locally, mirror the same Terraform version and backend settings to avoid drift.
- Review any planned changes carefully before applying — infrastructure changes may be destructive.
- Use CI secrets and encrypted state backends for production infrastructure. The repo-level workflows expect environment inputs and will run against the environment folder you choose.

If you are unsure which environment to run or how backends are configured, consult the CI workflow at `.github/workflows/runTerraform.yml` and contact the repository maintainers for guidance.

# Contributing
This project has the same [contribution policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#Contributing) as the main repository.

# License
This project has the same [license policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#License) as the main repository.