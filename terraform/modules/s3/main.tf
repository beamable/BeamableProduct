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


locals {
  s3_lightbeam_origin_id = "S3-${aws_s3_bucket.lightbeam.bucket}"
}

resource "aws_cloudfront_origin_access_identity" "lightbeam_s3" {
  comment = "CloudFront access to ${local.s3_lightbeam_origin_id}"
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
        Sid: "BucketReadObjects",
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
