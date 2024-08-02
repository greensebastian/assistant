variable "base_config" {
  type = object({
    environment = string
    location = string
    project = string
})
}

variable "tags" {
  type = map(string)
  default = {}
}

module "location" {
  source = "../location"
  location = var.base_config.location
}

locals {
  merged_tags = merge(var.tags, {
    terraform = "true"
    environment = var.base_config.environment
    location = module.location.standard
    locationShort = module.location.short
  })
}

output "tags" {
  value = merge(var.tags, {
    terraform = "true"
    project = var.base_config.project
    environment = var.base_config.environment
    location = module.location.standard
    locationShort = module.location.short
  })
}
