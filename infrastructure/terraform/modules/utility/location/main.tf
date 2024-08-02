variable "location" {
  type = string
}

module "region" {
  source = "git@github.com:greensebastian/terraform-azurerm-region-claranet.git"
  azure_region = var.location
}

output "standard" {
  value = module.region.location
}

output "short" {
  value = module.region.location_short
}
