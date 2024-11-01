﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace _301230968_Zhang__Lab03.Models
{
    public partial class User
    {
        [Key]
        [Required(ErrorMessage = "Email is required.")]
        [StringLength(50, ErrorMessage = "Email cannot be longer than 50 characters.")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email.")]
        public string UserId { get; set; } = null!;

        [Required(ErrorMessage = "User Name is required.")]
        [StringLength(50, ErrorMessage = "User Name cannot be longer than 50 characters.")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(50, ErrorMessage = "Password cannot be longer than 50 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "First Name cannot be empty.")]
        [StringLength(100, ErrorMessage = "First Name cannot be longer than 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last Name cannot be empty.")]
        [StringLength(100, ErrorMessage = "Last Name cannot be longer than 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Address cannot be empty.")]
        [StringLength(255, ErrorMessage = "Address cannot be longer than 255 characters.")]
        public string Address { get; set; } = null!;

        [Required(ErrorMessage = "Phone Number cannot be empty.")]
        [StringLength(12, ErrorMessage = "Phone Number cannot be longer than 12 characters.")]
        [RegularExpression(@"^\+?\d{0,3}?[-. ]?\(?\d{1,4}?\)?[-. ]?\d{1,4}[-. ]?\d{1,9}$", ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = null!;


        public DateTime RegisterDate { get; set; }
    }
}
