terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 4.0"
    }
  }
}

variable "ROOT_DOMAIN_NAME" {
  type    = string
}

variable "HOSTED_ZONE_ID" {
  type    = string
}

variable "CERTIFICATE" {
  type    = string
}

module "s3" {
  source              = "../../modules/s3"
  hosted_zone         = var.HOSTED_ZONE_ID
  lightbeam_domain_name = "lightbeams.${var.ROOT_DOMAIN_NAME}"
  certificate         = var.CERTIFICATE
  lightbeam_price_class = "PriceClass_100"
}