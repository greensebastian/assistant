module "bootstrap"{
  source = "../../../modules/bootstrap"

  base_config = {
    environment = "dev"
    location = "West Europe"
    project = "assistant"
  }
}
