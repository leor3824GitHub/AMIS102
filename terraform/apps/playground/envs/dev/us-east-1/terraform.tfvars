################################################################################
# Environment Settings
################################################################################

environment = "dev"
region      = "us-east-1"

################################################################################
# Network Configuration
################################################################################

vpc_cidr_block = "10.10.0.0/16"

public_subnets = {
  a = {
    cidr_block = "10.10.0.0/24"
    az         = "us-east-1a"
  }
  b = {
    cidr_block = "10.10.1.0/24"
    az         = "us-east-1b"
  }
}

private_subnets = {
  a = {
    cidr_block = "10.10.10.0/24"
    az         = "us-east-1a"
  }
  b = {
    cidr_block = "10.10.11.0/24"
    az         = "us-east-1b"
  }
}

# Use single NAT gateway in dev to save costs
enable_nat_gateway = true
single_nat_gateway = true

# VPC Endpoints (reduce data transfer costs)
enable_s3_endpoint   = true
enable_ecr_endpoints = true
enable_logs_endpoint = true

################################################################################
# S3 Configuration
################################################################################

app_s3_bucket_name        = "dev-AMIS-app-bucket"
app_s3_enable_public_read = false
app_s3_enable_cloudfront  = true

################################################################################
# Database Configuration
################################################################################

db_name     = "AMISdb"
db_username = "AMISadmin"

# Option 1: Use AWS Secrets Manager for password (recommended)
db_manage_master_user_password = true

# Option 2: Provide password directly (not recommended, use TF_VAR_db_password env var)
# db_password = "your-secure-password"

################################################################################
# Container Images
################################################################################

# Single tag for all container images (typically a git commit SHA or version)
container_image_tag = "1d2c9f9d3b85bb86229f1bc1b9cd8196054f2166"

# Optional: Override defaults if needed
# container_registry = "ghcr.io/amis"
# api_image_name     = "AMIS-playground-api"
# blazor_image_name  = "AMIS-playground-blazor"

################################################################################
# Service Configuration (dev defaults use Fargate Spot for cost savings)
################################################################################

api_desired_count    = 1
api_use_fargate_spot = true

blazor_desired_count    = 1
blazor_use_fargate_spot = true
