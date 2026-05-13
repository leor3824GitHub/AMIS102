################################################################################
# Environment Settings
################################################################################

environment = "staging"
region      = "us-east-1"

# Uncomment to enable HTTPS with a custom domain
# domain_name         = "staging.example.com"
# enable_https        = true
# acm_certificate_arn = "arn:aws:acm:us-east-1:ACCOUNT_ID:certificate/CERT_ID"

################################################################################
# Network Configuration
################################################################################

vpc_cidr_block = "10.20.0.0/16"

public_subnets = {
  a = {
    cidr_block = "10.20.0.0/24"
    az         = "us-east-1a"
  }
  b = {
    cidr_block = "10.20.1.0/24"
    az         = "us-east-1b"
  }
}

private_subnets = {
  a = {
    cidr_block = "10.20.10.0/24"
    az         = "us-east-1a"
  }
  b = {
    cidr_block = "10.20.11.0/24"
    az         = "us-east-1b"
  }
}

# Use single NAT gateway in staging to reduce costs
single_nat_gateway = true

# VPC Endpoints
enable_s3_endpoint             = true
enable_ecr_endpoints           = true
enable_logs_endpoint           = true
enable_secretsmanager_endpoint = true

# Enable flow logs for audit
enable_flow_logs = true

################################################################################
# S3 Configuration
################################################################################

app_s3_bucket_name        = "staging-AMIS-app-bucket"
app_s3_enable_public_read = false
app_s3_enable_cloudfront  = true

################################################################################
# Database Configuration
################################################################################

db_name     = "AMISdb"
db_username = "AMISadmin"

# Use AWS Secrets Manager for password (recommended)
db_manage_master_user_password = true

# Staging uses a larger instance class
db_instance_class              = "db.t3.small"
db_enable_performance_insights = true
db_deletion_protection         = true

################################################################################
# Redis Configuration
################################################################################

redis_node_type = "cache.t3.small"

################################################################################
# Container Images
################################################################################

# Single tag for all container images
container_image_tag = "staging"

# Optional: Override defaults if needed
# container_registry = "ghcr.io/amis"
# api_image_name     = "AMIS-playground-api"
# blazor_image_name  = "AMIS-playground-blazor"

################################################################################
# Service Configuration
################################################################################

api_desired_count    = 2
api_use_fargate_spot = true

blazor_desired_count    = 2
blazor_use_fargate_spot = true

# Enable Container Insights for monitoring
enable_container_insights = true
