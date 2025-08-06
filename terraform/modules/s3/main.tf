terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 4.0"
    }
  }
}

# Lightbeam
resource "aws_s3_bucket" "lightbeam" {
  bucket = "beamable-lightbeams"

  tags = {
    Terraform   = true
  }
}

# Otel Collector
resource "aws_s3_bucket" "otel-collector-ch" {
  bucket = "beamable-otel-collector-ch"
  tags = {
    Terraform = true
  }
}

# WebSdkDocs Collector
resource "aws_s3_bucket" "websdkdocs" {
  bucket = "beamable-websdkdocs"
  tags = {
    Terraform = true
  }
}

locals {
  s3_lightbeam_origin_id = "S3-${aws_s3_bucket.lightbeam.bucket}"
  s3_websdkdocs_origin_id = "S3-${aws_s3_bucket.websdkdocs.bucket}"
  s3_collector_origin_id = "S3-${aws_s3_bucket.otel-collector-ch.bucket}"
}

resource "aws_cloudfront_origin_access_identity" "lightbeam_s3" {
  comment = "CloudFront access to ${local.s3_lightbeam_origin_id}"
}

resource "aws_cloudfront_origin_access_identity" "websdkdocs_s3" {
  comment = "CloudFront access to ${local.s3_websdkdocs_origin_id}"
}

resource "aws_cloudfront_origin_access_identity" "collector_s3" {
  comment = "CloudFront access to ${local.s3_collector_origin_id}"
}

# Lightbeam Cloudfront
resource "aws_cloudfront_distribution" "lightbeam" {
  comment = "Beamable Lightbeams"
  aliases = [var.lightbeam_domain_name]
  enabled = true

  origin {
    domain_name = aws_s3_bucket.lightbeam.bucket_regional_domain_name
    origin_id   = local.s3_lightbeam_origin_id

    s3_origin_config {
      origin_access_identity = aws_cloudfront_origin_access_identity.lightbeam_s3.cloudfront_access_identity_path
    }
  }

  default_cache_behavior {
    allowed_methods  = ["GET", "HEAD", "OPTIONS"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = local.s3_lightbeam_origin_id

    min_ttl     = 0
    default_ttl = 0
    max_ttl     = 0

    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
    }

    viewer_protocol_policy = "redirect-to-https"
  }

  price_class = var.lightbeam_price_class

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn = var.certificate
    ssl_support_method  = "sni-only"
  }
}

resource "aws_route53_record" "lightbeam_record" {
  zone_id = var.hosted_zone
  name    = var.lightbeam_domain_name
  type    = "CNAME"
  ttl     = 300
  records = [aws_cloudfront_distribution.lightbeam.domain_name]
}


resource "aws_s3_bucket_policy" "lightbeam" {
  bucket = aws_s3_bucket.lightbeam.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid= "BucketReadObjects",
        Effect = "Allow",
        Principal = "*",
        Action = "*",
        Resource = [
          "${aws_s3_bucket.lightbeam.arn}/*",
          "${aws_s3_bucket.lightbeam.arn}"
        ]
      }
    ]
  })
}

resource "aws_s3_bucket_lifecycle_configuration" "lightbeam" {
  bucket = aws_s3_bucket.lightbeam.id
  rule {
    id = "nightly retention"
    expiration {
      days = 7
    }

    filter {
      prefix = "version/night-"
    }
    status = "Enabled"
  }
}

resource "aws_s3_bucket_public_access_block" "lightbeam" {
  bucket = aws_s3_bucket.lightbeam.id

  block_public_acls       = false
  block_public_policy     = false
  ignore_public_acls      = false
  restrict_public_buckets = false
}

resource "aws_s3_bucket_ownership_controls" "lightbeam" {
  bucket = aws_s3_bucket.lightbeam.id
  rule {
    object_ownership = "BucketOwnerPreferred"
  }
}

resource "aws_s3_bucket_cors_configuration" "lightbeam" {
  bucket = aws_s3_bucket.lightbeam.id
  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "PUT", "POST", "DELETE"]
    allowed_origins = ["*"]
    expose_headers  = ["x-amz-server-side-encryption", "x-amz-request-id", "x-amz-id-2"]
    max_age_seconds = 3000
  }
}

resource "aws_s3_bucket_acl" "lightbeam" {
  bucket = aws_s3_bucket.lightbeam.id
  acl    = "private"
  depends_on = [aws_s3_bucket_ownership_controls.lightbeam]
}


# websdkdocs Cloudfront
resource "aws_cloudfront_distribution" "websdkdocs" {
  comment = "Beamable websdkdocs"
  aliases = [var.websdkdocs_domain_name]
  enabled = true

  origin {
    domain_name = aws_s3_bucket.websdkdocs.bucket_regional_domain_name
    origin_id   = local.s3_websdkdocs_origin_id

    s3_origin_config {
      origin_access_identity = aws_cloudfront_origin_access_identity.websdkdocs_s3.cloudfront_access_identity_path
    }
  }

  default_cache_behavior {
    allowed_methods  = ["GET", "HEAD", "OPTIONS"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = local.s3_websdkdocs_origin_id

    min_ttl     = 0
    default_ttl = 0
    max_ttl     = 0

    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
    }

    viewer_protocol_policy = "redirect-to-https"
  }

  price_class = var.websdkdocs_price_class

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn = var.certificate
    ssl_support_method  = "sni-only"
  }
}

resource "aws_route53_record" "websdkdocs_record" {
  zone_id = var.hosted_zone
  name    = var.websdkdocs_domain_name
  type    = "CNAME"
  ttl     = 300
  records = [aws_cloudfront_distribution.websdkdocs.domain_name]
}


resource "aws_s3_bucket_policy" "websdkdocs" {
  bucket = aws_s3_bucket.websdkdocs.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid= "BucketReadObjects",
        Effect = "Allow",
        Principal = "*",
        Action = "*",
        Resource = [
          "${aws_s3_bucket.websdkdocs.arn}/*",
          "${aws_s3_bucket.websdkdocs.arn}"
        ]
      }
    ]
  })
}

resource "aws_s3_bucket_lifecycle_configuration" "websdkdocs" {
  bucket = aws_s3_bucket.websdkdocs.id

  rule {
    id = "default"
    status = "Enabled"
  }
}

resource "aws_s3_bucket_public_access_block" "websdkdocs" {
  bucket = aws_s3_bucket.websdkdocs.id

  block_public_acls       = false
  block_public_policy     = false
  ignore_public_acls      = false
  restrict_public_buckets = false
}

resource "aws_s3_bucket_ownership_controls" "websdkdocs" {
  bucket = aws_s3_bucket.websdkdocs.id
  rule {
    object_ownership = "BucketOwnerPreferred"
  }
}

resource "aws_s3_bucket_cors_configuration" "websdkdocs" {
  bucket = aws_s3_bucket.websdkdocs.id
  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "PUT", "POST", "DELETE"]
    allowed_origins = ["*"]
    expose_headers  = ["x-amz-server-side-encryption", "x-amz-request-id", "x-amz-id-2"]
    max_age_seconds = 3000
  }
}

resource "aws_s3_bucket_acl" "websdkdocs" {
  bucket = aws_s3_bucket.websdkdocs.id
  acl    = "private"
  depends_on = [aws_s3_bucket_ownership_controls.websdkdocs]
}





# Collector Cloudfront
resource "aws_cloudfront_distribution" "collector" {
  comment = "Beamable Clickhouse Otel Collectors"
  aliases = [var.collector_domain_name]
  enabled = true

  origin {
    domain_name = aws_s3_bucket.otel-collector-ch.bucket_regional_domain_name
    origin_id   = local.s3_collector_origin_id

    s3_origin_config {
      origin_access_identity = aws_cloudfront_origin_access_identity.collector_s3.cloudfront_access_identity_path
    }
  }

  default_cache_behavior {
    allowed_methods  = ["GET", "HEAD", "OPTIONS"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = local.s3_collector_origin_id

    min_ttl     = 0
    default_ttl = 0
    max_ttl     = 0

    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
    }

    viewer_protocol_policy = "redirect-to-https"
  }

  price_class = var.collector_price_class

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn = var.certificate
    ssl_support_method  = "sni-only"
  }
}

resource "aws_route53_record" "collector_record" {
  zone_id = var.hosted_zone
  name    = var.collector_domain_name
  type    = "CNAME"
  ttl     = 300
  records = [aws_cloudfront_distribution.collector.domain_name]
}


resource "aws_s3_bucket_policy" "collector" {
  bucket = aws_s3_bucket.otel-collector-ch.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid= "BucketReadObjects",
        Effect = "Allow",
        Principal = "*",
        Action = "*",
        Resource = [
          "${aws_s3_bucket.otel-collector-ch.arn}/*",
          "${aws_s3_bucket.otel-collector-ch.arn}"
        ]
      }
    ]
  })
}


resource "aws_s3_bucket_public_access_block" "collector" {
  bucket = aws_s3_bucket.otel-collector-ch.id

  block_public_acls       = false
  block_public_policy     = false
  ignore_public_acls      = false
  restrict_public_buckets = false
}

resource "aws_s3_bucket_ownership_controls" "collector" {
  bucket = aws_s3_bucket.otel-collector-ch.id
  rule {
    object_ownership = "BucketOwnerPreferred"
  }
}

resource "aws_s3_bucket_cors_configuration" "collector" {
  bucket = aws_s3_bucket.otel-collector-ch.id
  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "PUT", "POST", "DELETE"]
    allowed_origins = ["*"]
    expose_headers  = ["x-amz-server-side-encryption", "x-amz-request-id", "x-amz-id-2"]
    max_age_seconds = 3000
  }
}

resource "aws_s3_bucket_acl" "collector" {
  bucket = aws_s3_bucket.otel-collector-ch.id
  acl    = "private"
  depends_on = [aws_s3_bucket_ownership_controls.collector]
}
