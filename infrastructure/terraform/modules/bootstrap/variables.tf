variable "base_config" {
  type = object({
    environment = string
    location = string
    project = string
  })
}
