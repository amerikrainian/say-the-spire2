mod cli;
mod core;
mod gui;

fn main() {
    if std::env::args().any(|a| a == "--cli") {
        cli::run();
    } else {
        gui::run();
    }
}
