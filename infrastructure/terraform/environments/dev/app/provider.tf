terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
      version = ">= 3"
    }
  }
}

variable "azurerm_tenant_id" {
    type = string
}

variable "azurerm_subscription_id" {
    type = string
}

provider "azurerm" {
    features {}

    tenant_id = var.azurerm_tenant_id
    subscription_id = var.azurerm_subscription_id
}