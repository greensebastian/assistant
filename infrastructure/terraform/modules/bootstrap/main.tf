module "location" {
  source = "../utility/location"
  location = var.base_config.location
}

locals {
  location_short = module.location.short
}

module "azurerm_names_bootstrap_terraform" {
  source = "git@github.com:greensebastian/terraform-azurerm-naming.git"
  suffix = [ var.base_config.project, local.location_short, var.base_config.environment, "bootstrap" ]
}

module "tags" {
  source = "../utility/tags"
  base_config = var.base_config
}

resource "azurerm_resource_group" "bootstrap_terraform" {
  name = module.azurerm_names_bootstrap_terraform.resource_group.name
  location = var.base_config.location
  tags = module.tags.tags
}

resource "azurerm_storage_account" "bootstrap_terraform" {
  name = module.azurerm_names_bootstrap_terraform.storage_account.name
  resource_group_name = azurerm_resource_group.bootstrap_terraform.name
  location = azurerm_resource_group.bootstrap_terraform.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  tags = module.tags.tags
}

resource "azurerm_storage_container" "bootstrap_terraform" {
  name                  = module.azurerm_names_bootstrap_terraform.storage_container.name
  storage_account_name  = azurerm_storage_account.bootstrap_terraform.name
  container_access_type = "private"
}

resource "azurerm_storage_blob" "bootstrap_terraform" {
  name                   = module.azurerm_names_bootstrap_terraform.storage_blob.name
  storage_account_name   = azurerm_storage_account.bootstrap_terraform.name
  storage_container_name = azurerm_storage_container.bootstrap_terraform.name
  type                   = "Block"
}

module "azurerm_names_bootstrap" {
  source = "git@github.com:greensebastian/terraform-azurerm-naming.git"
  suffix = [ var.base_config.project, local.location_short, var.base_config.environment ]
}

resource "azurerm_resource_group" "bootstrap" {
  name = module.azurerm_names_bootstrap.resource_group.name
  location = var.base_config.location
  tags = module.tags.tags
}
