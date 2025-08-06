variable "hosted_zone" {
  type        = string
  description = "Hosted zone that DNS validation entries will be created in."
}

variable "certificate" {
  type        = string
  description = "ARN of the SSL/TLS certificate for the domain name."
}

variable "lightbeam_domain_name" {
  type        = string
  description = "Fully qualified domain name for hosting Lightbeams, like 'lightbeams.beamable.com'."
}

variable "collector_domain_name" {
  type        = string
  description = "Fully qualified domain name for hosting Otel Collectors, like 'collectors.beamable.com'."
}

variable "websdkdocs_domain_name" {
  type        = string
  description = "Fully qualified domain name for hosting web sdk docs, like 'websdkdocs.beamable.com'."
}

variable "lightbeam_price_class" {
  type        = string
  description = "CloudFront price class: PriceClass_Any (global), PriceClass_200 (most regions), or PriceClass_100 (US, Canada, Europe)"
  validation {
    condition     = contains(["PriceClass_All", "PriceClass_200", "PriceClass_100"], var.lightbeam_price_class)
    error_message = "Price class must be one of [PriceClass_All, PriceClass_200, PriceClass_100]."
  }
  default = "PriceClass_100"
}

variable "collector_price_class" {
  type        = string
  description = "CloudFront price class: PriceClass_Any (global), PriceClass_200 (most regions), or PriceClass_100 (US, Canada, Europe)"
  validation {
    condition     = contains(["PriceClass_All", "PriceClass_200", "PriceClass_100"], var.collector_price_class)
    error_message = "Price class must be one of [PriceClass_All, PriceClass_200, PriceClass_100]."
  }
  default = "PriceClass_100"
}

variable "websdkdocs_price_class" {
  type        = string
  description = "CloudFront price class: PriceClass_Any (global), PriceClass_200 (most regions), or PriceClass_100 (US, Canada, Europe)"
  validation {
    condition     = contains(["PriceClass_All", "PriceClass_200", "PriceClass_100"], var.websdkdocs_price_class)
    error_message = "Price class must be one of [PriceClass_All, PriceClass_200, PriceClass_100]."
  }
  default = "PriceClass_100"
}