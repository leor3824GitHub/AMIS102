# Terraform 1.10+ uses S3 native locking via use_lockfile.
# DynamoDB is no longer required for state locking.
terraform {
  backend "s3" {
    bucket       = "AMIS-state-bucket"
    key          = "staging/us-east-1/terraform.tfstate"
    region       = "us-east-1"
    encrypt      = true
    use_lockfile = true
  }
}
