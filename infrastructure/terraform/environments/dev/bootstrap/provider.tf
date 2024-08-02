terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
      version = ">= 3"
    }
  }

  backend "azurerm" {
    resource_group_name = "rg-assistant-euw-dev-bootstrap"  # Replace with your resource group name
    storage_account_name = "stassistanteuwdevbootstr"  # Replace with your storage account name
    container_name = "stct-assistant-euw-dev-bootstrap"  # Replace with your desired container name
    key = "terraform.tfstate"  # Optional: Specify the filename within the container (defaults to 'terraform.tfstate')
  }
}

provider "azurerm" {
    features {}
}
