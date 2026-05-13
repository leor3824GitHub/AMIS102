################################################################################
# Environment Settings
################################################################################

environment = "prod"
region      = "us-east-1"

# Configure with your production domain
# domain_name         = "app.example.com"
# enable_https        = true
# acm_certificate_arn = "arn:aws:acm:us-east-1:ACCOUNT_ID:certificate/CERT_ID"

################################################################################
# Network Configuration
################################################################################

vpc_cidr_block = "10.30.0.0/16"

public_subnets = {
  a = {
    cidr_block = "10.30.0.0/24"
    az         = "us-east-1a"
  }
  b = {
    cidr_block = "10.30.1.0/24"
    az         = "us-east-1b"
  }
  c = {
    cidr_block = "10.30.2.0/24"
    az         = "us-east-1c"
  }
}

private_subnets = {
  a = {
    cidr_block = "10.30.10.0/24"
    az         = "us-east-1a"
  }
  b = {
    cidr_block = "10.30.11.0/24"
    az         = "us-east-1b"
  }
  c = {
    cidr_block = "10.30.12.0/24"
    az         = "us-east-1c"
  }
}

# Production uses NAT gateway per AZ for high availability
single_nat_gateway = false

# VPC Endpoints for security and performance
enable_s3_endpoint             = true
enable_ecr_endpoints           = true
enable_logs_endpoint           = true
enable_secretsmanager_endpoint = true

# Enable flow logs for compliance and security auditing
enable_flow_logs         = true
flow_logs_retention_days = 90

################################################################################
# S3 Configuration
################################################################################

app_s3_bucket_name                = "prod-AMIS-app-bucket"
app_s3_versioning_enabled         = true
app_s3_enable_public_read         = false
app_s3_enable_cloudfront          = true
app_s3_cloudfront_price_class     = "PriceClass_200"
app_s3_enable_intelligent_tiering = true

################################################################################
# Database Configuration
################################################################################

db_name     = "AMISdb"
db_username = "AMISadmin"

# Use AWS Secrets Manager for password (mandatory for production)
db_manage_master_user_password = true

# Production database settings
db_instance_class              = "db.t3.medium"
db_allocated_storage           = 50
db_max_allocated_storage       = 200
db_multi_az                    = true
db_backup_retention_period     = 30
db_deletion_protection         = true
db_enable_performance_insights = true
db_enable_enhanced_monitoring  = true

################################################################################
# Redis Configuration
################################################################################

redis_node_type                  = "cache.t3.medium"
redis_num_cache_clusters         = 2
redis_automatic_failover_enabled = true

################################################################################
# Container Images
################################################################################

# Single tag for all container images
container_image_tag = "latest"

# Optional: Override defaults if needed
# container_registry = "ghcr.io/amis"
# api_image_name     = "AMIS-playground-api"
# blazor_image_name  = "AMIS-playground-blazor"

################################################################################
# Service Configuration (Production - no Spot for stability)
################################################################################

api_desired_count    = 3
api_use_fargate_spot = false

blazor_desired_count    = 3
blazor_use_fargate_spot = false

# Enable Container Insights for full observability
enable_container_insights = true

# ALB protection
alb_enable_deletion_protection = true
