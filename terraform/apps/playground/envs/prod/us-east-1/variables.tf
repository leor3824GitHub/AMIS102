################################################################################
# General Variables
################################################################################

variable "environment" {
  type        = string
  description = "Environment name."
  default     = "prod"
}

variable "region" {
  type        = string
  description = "AWS region."
  default     = "us-east-1"
}

variable "domain_name" {
  type        = string
  description = "Domain name for the application."
  default     = null
}

################################################################################
# Network Variables
################################################################################

variable "vpc_cidr_block" {
  type        = string
  description = "CIDR block for the VPC."
}

variable "public_subnets" {
  description = "Public subnet definitions."
  type = map(object({
    cidr_block = string
    az         = string
  }))
}

variable "private_subnets" {
  description = "Private subnet definitions."
  type = map(object({
    cidr_block = string
    az         = string
  }))
}

variable "enable_nat_gateway" {
  type        = bool
  description = "Enable NAT Gateway."
  default     = true
}

variable "single_nat_gateway" {
  type        = bool
  description = "Use single NAT Gateway."
  default     = false
}

variable "enable_s3_endpoint" {
  type        = bool
  description = "Enable S3 VPC Gateway Endpoint."
  default     = true
}

variable "enable_ecr_endpoints" {
  type        = bool
  description = "Enable ECR VPC Interface Endpoints."
  default     = true
}

variable "enable_logs_endpoint" {
  type        = bool
  description = "Enable CloudWatch Logs VPC Interface Endpoint."
  default     = true
}

variable "enable_secretsmanager_endpoint" {
  type        = bool
  description = "Enable Secrets Manager VPC Interface Endpoint."
  default     = true
}

variable "enable_flow_logs" {
  type        = bool
  description = "Enable VPC Flow Logs."
  default     = true
}

variable "flow_logs_retention_days" {
  type        = number
  description = "Flow logs retention period in days."
  default     = 90
}

################################################################################
# ECS Variables
################################################################################

variable "enable_container_insights" {
  type        = bool
  description = "Enable Container Insights."
  default     = true
}

################################################################################
# ALB Variables
################################################################################

variable "enable_https" {
  type        = bool
  description = "Enable HTTPS on ALB."
  default     = true
}

variable "acm_certificate_arn" {
  type        = string
  description = "ACM certificate ARN for HTTPS."
  default     = null
}

variable "alb_enable_deletion_protection" {
  type        = bool
  description = "Enable deletion protection for ALB."
  default     = true
}

################################################################################
# S3 Variables
################################################################################

variable "app_s3_bucket_name" {
  type        = string
  description = "S3 bucket for application data."
}

variable "app_s3_versioning_enabled" {
  type        = bool
  description = "Enable versioning on S3 bucket."
  default     = true
}

variable "app_s3_enable_public_read" {
  type        = bool
  description = "Whether to enable public read on uploads prefix."
  default     = false
}

variable "app_s3_enable_cloudfront" {
  type        = bool
  description = "Whether to enable CloudFront in front of the app bucket."
  default     = true
}

variable "app_s3_cloudfront_price_class" {
  type        = string
  description = "Price class for CloudFront distribution."
  default     = "PriceClass_200"
}

variable "app_s3_enable_intelligent_tiering" {
  type        = bool
  description = "Enable automatic transition to Intelligent-Tiering."
  default     = true
}

################################################################################
# Database Variables
################################################################################

variable "db_name" {
  type        = string
  description = "Database name."
}

variable "db_username" {
  type        = string
  description = "Database admin username."
}

variable "db_password" {
  type        = string
  description = "Database admin password."
  sensitive   = true
  default     = null
}

variable "db_manage_master_user_password" {
  type        = bool
  description = "Let AWS manage the master user password in Secrets Manager."
  default     = true
}

variable "db_instance_class" {
  type        = string
  description = "RDS instance class."
  default     = "db.t3.medium"
}

variable "db_allocated_storage" {
  type        = number
  description = "Allocated storage in GB."
  default     = 50
}

variable "db_max_allocated_storage" {
  type        = number
  description = "Maximum allocated storage for autoscaling in GB."
  default     = 200
}

variable "db_engine_version" {
  type        = string
  description = "PostgreSQL engine version."
  default     = "16"
}

variable "db_multi_az" {
  type        = bool
  description = "Enable Multi-AZ deployment."
  default     = true
}

variable "db_backup_retention_period" {
  type        = number
  description = "Backup retention period in days."
  default     = 30
}

variable "db_deletion_protection" {
  type        = bool
  description = "Enable deletion protection."
  default     = true
}

variable "db_enable_performance_insights" {
  type        = bool
  description = "Enable Performance Insights."
  default     = true
}

variable "db_enable_enhanced_monitoring" {
  type        = bool
  description = "Enable Enhanced Monitoring."
  default     = true
}

################################################################################
# Redis Variables
################################################################################

variable "redis_node_type" {
  type        = string
  description = "ElastiCache node type."
  default     = "cache.t3.medium"
}

variable "redis_num_cache_clusters" {
  type        = number
  description = "Number of cache clusters (nodes)."
  default     = 2
}

variable "redis_automatic_failover_enabled" {
  type        = bool
  description = "Enable automatic failover."
  default     = true
}

################################################################################
# Container Image Variables
################################################################################

variable "container_registry" {
  type        = string
  description = "Container registry URL."
  default     = "ghcr.io/amis"
}

variable "container_image_tag" {
  type        = string
  description = "Container image tag (shared across all services)."
}

variable "api_image_name" {
  type        = string
  description = "API container image name."
  default     = "AMIS-playground-api"
}

variable "blazor_image_name" {
  type        = string
  description = "Blazor container image name."
  default     = "AMIS-playground-blazor"
}

################################################################################
# API Service Variables
################################################################################

variable "api_container_port" {
  type        = number
  description = "API container port."
  default     = 8080
}

variable "api_cpu" {
  type        = string
  description = "API CPU units."
  default     = "1024"
}

variable "api_memory" {
  type        = string
  description = "API memory."
  default     = "2048"
}

variable "api_desired_count" {
  type        = number
  description = "Desired API task count."
  default     = 3
}

variable "api_enable_circuit_breaker" {
  type        = bool
  description = "Enable deployment circuit breaker."
  default     = true
}

variable "api_use_fargate_spot" {
  type        = bool
  description = "Use Fargate Spot capacity."
  default     = false
}

################################################################################
# Blazor Service Variables
################################################################################

variable "blazor_container_port" {
  type        = number
  description = "Blazor container port."
  default     = 8080
}

variable "blazor_cpu" {
  type        = string
  description = "Blazor CPU units."
  default     = "1024"
}

variable "blazor_memory" {
  type        = string
  description = "Blazor memory."
  default     = "2048"
}

variable "blazor_desired_count" {
  type        = number
  description = "Desired Blazor task count."
  default     = 3
}

variable "blazor_enable_circuit_breaker" {
  type        = bool
  description = "Enable deployment circuit breaker."
  default     = true
}

variable "blazor_use_fargate_spot" {
  type        = bool
  description = "Use Fargate Spot capacity."
  default     = false
}
